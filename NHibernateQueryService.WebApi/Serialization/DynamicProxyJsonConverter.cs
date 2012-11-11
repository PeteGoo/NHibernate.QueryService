using System;
using System.Collections;
using System.Linq;
using NHibernate.Collection;
using NHibernate.Collection.Generic;
using NHibernate.Proxy;
using NHibernate.Proxy.DynamicProxy;
using Newtonsoft.Json;

namespace NHibernateQueryService.WebApi.Serialization {
    public class DynamicProxyJsonConverter : JsonConverter {
        

        public DynamicProxyJsonConverter() {
        
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter"/> to write to.</param><param name="value">The value.</param><param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            if (value is AbstractPersistentCollection && ((AbstractPersistentCollection)value).WasInitialized) {
                writer.WriteStartArray();
                foreach(var item in ((IEnumerable)value)){
                    serializer.Serialize(writer, item);
                }
                return;
            }
            writer.WriteNull();
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader"/> to read from.</param><param name="objectType">Type of the object.</param><param name="existingValue">The existing value of object being read.</param><param name="serializer">The calling serializer.</param>
        /// <returns>
        /// The object value.
        /// </returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType) {
            if (typeof(AbstractPersistentCollection).IsAssignableFrom(objectType)) {
                return true;
            }

            if( objectType.Name.EndsWith("Proxy") &&
                              (objectType.GetInterfaces().Any(
                                  iface => iface == typeof (INHibernateProxy) || iface == typeof (IProxy)))) {
                return true;
            }

            

            return false;
            
        }
    }
}