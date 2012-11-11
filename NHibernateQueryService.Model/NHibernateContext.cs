using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Services;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Metadata;

namespace NHibernateQueryService.Model {

    public class QueryServiceContext : NHibernateContext {
        public IQueryable<Person> People{
            get { return ApplyExpansions<Person>(Session.Query<Person>().Cacheable().CacheRegion("People")); }
        }

        public IQueryable<Address> Addresses {
            get { return NHibernateExtensions.ProcessExpands<Address>(Session.Query<Address>()); }
        }

        public IQueryable<Country> Countries {
            get { return NHibernateExtensions.ProcessExpands<Country>(Session.Query<Country>().Cacheable().CacheRegion("Countries")); }
        }

        protected override ISession ProvideSession() {
            return NHibernateHelper.OpenSession();
        }
    }

    public class NHibernateContext : IExpandProvider {
        private ISession session;

        /// <summary>
        /// Type for the class that contains all *Fetch* functions we need for $expand
        /// </summary>
        private static readonly System.Type EagerFetchingExtensionMethodsType = typeof(EagerFetchingExtensionMethods);

        /// <summary>
        /// Initializes a new instance of the <see cref="NHibernateContext"/> class.
        /// </summary>
        public NHibernateContext() {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NHibernate.Linq.NHibernateContext"/> class.
        /// </summary>
        /// <param name="session">An initialized <see cref="T:NHibernate.ISession"/> object.</param>
        public NHibernateContext(ISession session) {
            this.session = session;
        }

        /// <summary>
        /// Gets a reference to the <see cref="T:NHibernate.ISession"/> associated with this object.
        /// </summary>
        public virtual ISession Session {
            get {
                if (session == null) {
                    // Attempt to get the Session
                    session = ProvideSession();
                }
                return session;
            }
        }



        /// <summary>
        /// Allows for empty construction but provides an interface for an interface to have the derived 
        /// classes provide a session object late in the cycle. 
        /// </summary>
        /// <returns>The Required <see cref="T:NHibernate.ISession"/> object.</returns>
        protected virtual ISession ProvideSession() {
            // Should not be called as supplying the session in the constructor
            throw new NotImplementedException("If NHibernateContext is constructed with the empty constructor, inheritor is required to override ProvideSession to supply Session.");
        }

        #region ICloneable Members

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns></returns>
        public virtual object Clone() {
            if (Session == null) {
                throw new ArgumentNullException("Session");
            }

            return Activator.CreateInstance(GetType(), new object[]{Session});
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Disposes the wrapped <see cref="T:NHibernate.ISession"/> object.
        /// </summary>
        public virtual void Dispose() {
            if (session != null) {
                session.Dispose();
                session = null;
            }
        }

        #endregion

        protected IQueryable<T> ApplyExpansions<T>(IQueryable<T> queryable) {
            string expandsQueryString = HttpContext.Current.Request.QueryString["$expand"];
            if (string.IsNullOrWhiteSpace(expandsQueryString)) {
                return queryable;
            }

            string[] expandPaths = expandsQueryString.Split(',').Select(s => s.Trim()).ToArray();

            if (queryable == null) throw new DataServiceException("Query cannot be null");

            var nHibQuery = queryable.Provider as NHibernate.Linq.DefaultQueryProvider;
            if (nHibQuery == null) throw new DataServiceException("Expansion only supported on INHibernateQueryable queries");

            if (!expandPaths.Any()) throw new DataServiceException("Expansion Paths cannot be null");
            var currentQueryable = queryable;
            foreach (string expand in expandPaths) {
                // We always start with the resulting element type
                var currentType = currentQueryable.ElementType;
                var isFirstFetch = true;
                foreach (string seg in expand.Split('/')) {
                    IClassMetadata metadata = Session.SessionFactory.GetClassMetadata(currentType);
                    if (metadata == null) {
                        throw new DataServiceException("Type not recognized as a valid type for this Context");
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
                    currentQueryable = (IQueryable)fetchMethodInfo.Invoke(null, args) as IQueryable<T>;

                    currentType = propType;
                    isFirstFetch = false;
                }
            }

            return currentQueryable;
        }

        IEnumerable IExpandProvider.ApplyExpansions(IQueryable queryable, ICollection<ExpandSegmentCollection> expandPaths) {
            if (queryable == null) throw new DataServiceException("Query cannot be null");

            var nHibQuery = queryable.Provider as NHibernate.Linq.DefaultQueryProvider;
            if (nHibQuery == null) throw new DataServiceException("Expansion only supported on INHibernateQueryable queries");

            if (expandPaths.Count == 0) throw new DataServiceException("Expansion Paths cannot be null");
            var currentQueryable = queryable;
            foreach (ExpandSegmentCollection coll in expandPaths) {
                // We always start with the resulting element type
                var currentType = currentQueryable.ElementType;
                var isFirstFetch = true;
                foreach (ExpandSegment seg in coll) {
                    if (seg.HasFilter) {
                        throw new DataServiceException("NHibernate does not support Expansions with Filters");
                    } else {
                        IClassMetadata metadata = Session.SessionFactory.GetClassMetadata(currentType);
                        if (metadata == null) {
                            throw new DataServiceException("Type not recognized as a valid type for this Context");
                        }

                        // Gather information about the property
                        var propInfo = currentType.GetProperty(seg.Name);
                        var propType = propInfo.PropertyType;
                        var metaPropType = metadata.GetPropertyType(seg.Name);

                        // When this is the first segment of a path, we have to use Fetch instead of ThenFetch
                        var propFetchFunctionName = (isFirstFetch ? "Fetch" : "ThenFetch");

                        // The delegateType is a type for the lambda creation to create the correct return value
                        System.Type delegateType;

                        if (metaPropType.IsCollectionType) {
                            // We have to use "FetchMany" or "ThenFetchMany" when the target property is a collection
                            propFetchFunctionName += "Many";

                            // We only support IList<T> or something similar
                            propType = propType.GetGenericArguments().Single();
                            delegateType = typeof(Func<,>).MakeGenericType(currentType, typeof(IEnumerable<>).MakeGenericType(propType));
                        } else {
                            delegateType = typeof(Func<,>).MakeGenericType(currentType, propType);
                        }

                        // Get the correct extension method (Fetch, FetchMany, ThenFetch, or ThenFetchMany)
                        var fetchMethodInfo = EagerFetchingExtensionMethodsType.GetMethod(propFetchFunctionName, BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod);
                        var fetchMethodTypes = new List<System.Type>();
                        fetchMethodTypes.AddRange(currentQueryable.GetType().GetGenericArguments().Take(isFirstFetch ? 1 : 2));
                        fetchMethodTypes.Add(propType);
                        fetchMethodInfo = fetchMethodInfo.MakeGenericMethod(fetchMethodTypes.ToArray());

                        // Create an expression of type new delegateType(x => x.{seg.Name})
                        var exprParam = System.Linq.Expressions.Expression.Parameter(currentType, "x");
                        var exprProp = System.Linq.Expressions.Expression.Property((Expression) exprParam, (string) seg.Name);
                        var exprLambda = System.Linq.Expressions.Expression.Lambda(delegateType, exprProp, new System.Linq.Expressions.ParameterExpression[] { exprParam });

                        // Call the *Fetch* function
                        var args = new object[] { currentQueryable, exprLambda };
                        currentQueryable = (IQueryable)fetchMethodInfo.Invoke(null, args);

                        currentType = propType;
                    }
                    isFirstFetch = false;
                }
            }

            return currentQueryable;
        }

    }
}