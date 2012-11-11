using System.Data.Services.Common;

namespace NHibernateQueryService.Model {
    [DataServiceKey("Id")] 
    public class Country {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
    }
}