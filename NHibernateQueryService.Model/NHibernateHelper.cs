using NHibernate;
using NHibernate.Cfg;

namespace NHibernateQueryService.Model {
    public class NHibernateHelper {
        private static volatile ISessionFactory sessionFactory;
        private static volatile Configuration configuration;
        private static readonly object syncRoot = new object();

        public static ISessionFactory SessionFactory {
            get {
                if (sessionFactory == null) {
                    lock(syncRoot) {
                        if (sessionFactory == null) {
                            return sessionFactory = Configuration.BuildSessionFactory();
                        }
                    }
                }
                return sessionFactory;
            }
        }

        public static Configuration Configuration {
            get {
                if (configuration == null) {
                    lock (syncRoot) {
                        if (configuration == null) {
                            var newConfig = new Configuration();
                            newConfig.Configure();
                            newConfig.AddAssembly(typeof(Person).Assembly);
                            newConfig.Cache(properties => {
                                properties.UseQueryCache = true;
                                properties.Provider<NHibernate.Caches.SysCache2.SysCacheProvider>();
                            });
                            newConfig.EntityCache<Country>(properties => {
                                properties.RegionName = "Countries";
                                properties.Strategy = EntityCacheUsage.Readonly;
                            });
                            return configuration = newConfig;
                        }
                    }
                }
                return configuration;
            }
        }

        public static ISession OpenSession() {
            ISession session = SessionFactory.OpenSession();
            session.DefaultReadOnly = true;
            return session;
        }
    }

    
}