using System.Data.Services.Common;

namespace NHibernateQueryService.Model {
    [DataServiceKey("Id")] 
    public class Address {
        public virtual int Id { get; set; }
        public virtual string Line1 { get; set; }
        public virtual string Line2 { get; set; }
        public virtual string Line3 { get; set; }
        public virtual string City { get; set; }
        public virtual Country Country { get; set; }
    }
}