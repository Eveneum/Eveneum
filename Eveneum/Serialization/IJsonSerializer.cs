using System;
using Eveneum.Documents;

namespace Eveneum.Serialization
{
    public interface IJsonSerializer
    {
        object Serialize(object value);
        object Deserialize(object token, Type targetType);
        IEveneumDocument CreateDocument(string id, DocumentType documentType);
    }
}
