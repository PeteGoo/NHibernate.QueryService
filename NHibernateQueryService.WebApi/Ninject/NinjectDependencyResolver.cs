using System.Web.Http.Dependencies;
using Ninject;

namespace NHibernateQueryService.WebApi.Ninject {
    /// <summary>
    /// Web API Dependency Resolver for Ninject
    /// </summary>
    public class NinjectDependencyResolver : NinjectDependencyScope, IDependencyResolver {
        IKernel kernel;

        public NinjectDependencyResolver(IKernel kernel)
            : base(kernel) {
            this.kernel = kernel;
        }

        public IDependencyScope BeginScope() {
            return new NinjectDependencyScope(kernel.BeginBlock());
        }
    }
}