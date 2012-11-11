using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web;
using System.Web.Http.Filters;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Metadata;
using NHibernateQueryService.Model;

namespace NHibernateQueryService.WebApi.ActionFilters {
    public class NhQueryableEnumeratorAttribute : ActionFilterAttribute {
        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext) {
            ObjectContent content = actionExecutedContext.Response.Content as ObjectContent;
            
            if (content != null && content.Value is IQueryable && ((IQueryable)content.Value).Provider is DefaultQueryProvider) {

                var queryableResult = content.Value.As<IQueryable>();

                queryableResult = ApplyExpansions(queryableResult);

                queryableResult = queryableResult.Provider.Execute(queryableResult.Expression) as IQueryable;
                content.Value = queryableResult;
            }

            base.OnActionExecuted(actionExecutedContext);
        }

        protected IQueryable ApplyExpansions(IQueryable queryable) {
            string expandsQueryString = HttpContext.Current.Request.QueryString["$expand"];
            if (string.IsNullOrWhiteSpace(expandsQueryString)) {
                return queryable;
            }

            string[] expandPaths = expandsQueryString.Split(',').Select(s => s.Trim()).ToArray();

            if (queryable == null) throw new HttpException("Query cannot be null");

            var nHibQuery = queryable.Provider as NHibernate.Linq.DefaultQueryProvider;
            if (nHibQuery == null) throw new HttpException("Expansion only supported on INHibernateQueryable queries");

            if (!expandPaths.Any()) throw new HttpException("Expansion Paths cannot be null");
            var currentQueryable = queryable;
            foreach (string expand in expandPaths) {
                // We always start with the resulting element type
                var currentType = currentQueryable.ElementType;
                var isFirstFetch = true;
                foreach (string seg in expand.Split('/')) {
                    IClassMetadata metadata = NHibernateHelper.SessionFactory.GetClassMetadata(currentType);
                    if (metadata == null) {
                        throw new HttpException("Type not recognized as a valid type for this Context");
                    }

                    // Gather information about the property
                    var propInfo = currentType.GetProperty(seg);
                    var propType = propInfo.PropertyType;
                    var metaPropType = metadata.GetPropertyType(seg);

                    // When this is the first segment of a path, we have to use Fetch instead of ThenFetch
                    var propFetchFunctionName = (isFirstFetch ? "Fetch" : "ThenFetch");

                    // The delegateType is a type for the lambda creation to create the correct return value
                    System.Type delegateType;

                    if (metaPropType.IsCollectionType) {
                        // We have to use "FetchMany" or "ThenFetchMany" when the target property is a collection
                        propFetchFunctionName += "Many";

                        // We only support IList<T> or something similar
                        propType = propType.GetGenericArguments().Single();
                        delegateType = typeof(Func<,>).MakeGenericType(currentType,
                                                                        typeof(IEnumerable<>).MakeGenericType(propType));
                    } else {
                        delegateType = typeof(Func<,>).MakeGenericType(currentType, propType);
                    }

                    // Get the correct extension method (Fetch, FetchMany, ThenFetch, or ThenFetchMany)
                    var fetchMethodInfo = typeof(EagerFetchingExtensionMethods).GetMethod(propFetchFunctionName,
                                                                                      BindingFlags.Static |
                                                                                      BindingFlags.Public |
                                                                                      BindingFlags.InvokeMethod);
                    var fetchMethodTypes = new List<System.Type>();
                    fetchMethodTypes.AddRange(currentQueryable.GetType().GetGenericArguments().Take(isFirstFetch ? 1 : 2));
                    fetchMethodTypes.Add(propType);
                    fetchMethodInfo = fetchMethodInfo.MakeGenericMethod(fetchMethodTypes.ToArray());

                    // Create an expression of type new delegateType(x => x.{seg.Name})
                    var exprParam = System.Linq.Expressions.Expression.Parameter(currentType, "x");
                    var exprProp = System.Linq.Expressions.Expression.Property(exprParam, seg);
                    var exprLambda = System.Linq.Expressions.Expression.Lambda(delegateType, exprProp,
                                                                               new System.Linq.Expressions.
                                                                                   ParameterExpression[] { exprParam });

                    // Call the *Fetch* function
                    var args = new object[] { currentQueryable, exprLambda };
                    currentQueryable = (IQueryable)fetchMethodInfo.Invoke(null, args) as IQueryable;

                    currentType = propType;
                    isFirstFetch = false;
                }
            }

            return currentQueryable;
        }
    }
}