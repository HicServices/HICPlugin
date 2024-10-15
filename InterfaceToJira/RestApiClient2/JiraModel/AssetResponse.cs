using System;
using System.Collections.Generic;
using System.Text;

namespace InterfaceToJira.RestApiClient2.JiraModel;

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);


public class AssetResponse
{
    public string workspaceId { get; set; }
    public string globalId { get; set; }
    public string id { get; set; }
    public string label { get; set; }
    public string objectKey { get; set; }
    public AssetResponseAvatar avatar { get; set; }
    public AssetResponseObjectType objectType { get; set; }
    public DateTime created { get; set; }
    public DateTime updated { get; set; }
    public bool hasAvatar { get; set; }
    public long timestamp { get; set; }
    public List<AssetResponseAttribute> attributes { get; set; }
    public ExtendedInfo extendedInfo { get; set; }
    public AssetResponseLinks _links { get; set; }
    public string name { get; set; }
}

public class AssetResponseAttribute
{
    public string workspaceId { get; set; }
    public string globalId { get; set; }
    public string id { get; set; }
    public ObjectTypeAttribute objectTypeAttribute { get; set; }
    public string objectTypeAttributeId { get; set; }
    public List<ObjectAttributeValue> objectAttributeValues { get; set; }
    public string objectId { get; set; }
}

public class AssetResponseAvatar
{
    public string workspaceId { get; set; }
    public string url16 { get; set; }
    public string url48 { get; set; }
    public string url72 { get; set; }
    public string url144 { get; set; }
    public string url288 { get; set; }
    public string objectId { get; set; }
    public MediaClientConfig mediaClientConfig { get; set; }
}

public class AssetResponseDefaultType
{
    public int id { get; set; }
    public string name { get; set; }
}

public class ExtendedInfo
{
    public bool openIssuesExists { get; set; }
    public bool attachmentsExists { get; set; }
}

public class AssetResponseIcon
{
    public string id { get; set; }
    public string name { get; set; }
    public string url16 { get; set; }
    public string url48 { get; set; }
}

public class AssetResponseLinks
{
    public string self { get; set; }
}

public class AssetResponseMediaClientConfig
{
    public string clientId { get; set; }
    public string mediaBaseUrl { get; set; }
    public string mediaJwtToken { get; set; }
    public string fileId { get; set; }
}

public class AssetResponseObjectAttributeValue
{
    public object value { get; set; }
    public string displayValue { get; set; }
    public object searchValue { get; set; }
    public bool referencedType { get; set; }
    public ReferencedObject referencedObject { get; set; }
}

public class AssetResponseObjectType
{
    public string workspaceId { get; set; }
    public string globalId { get; set; }
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public Icon icon { get; set; }
    public int position { get; set; }
    public DateTime created { get; set; }
    public DateTime updated { get; set; }
    public int objectCount { get; set; }
    public string objectSchemaId { get; set; }
    public bool inherited { get; set; }
    public bool abstractObjectType { get; set; }
    public bool parentObjectTypeInherited { get; set; }
}

public class AssetResponseObjectTypeAttribute
{
    public string workspaceId { get; set; }
    public string globalId { get; set; }
    public string id { get; set; }
    public string name { get; set; }
    public bool label { get; set; }
    public DefaultType defaultType { get; set; }
    public bool editable { get; set; }
    public bool system { get; set; }
    public bool sortable { get; set; }
    public bool summable { get; set; }
    public bool indexed { get; set; }
    public int minimumCardinality { get; set; }
    public int maximumCardinality { get; set; }
    public bool removable { get; set; }
    public bool hidden { get; set; }
    public bool includeChildObjectTypes { get; set; }
    public bool uniqueAttribute { get; set; }
    public string options { get; set; }
    public int position { get; set; }
    public string description { get; set; }
    public string suffix { get; set; }
    public string regexValidation { get; set; }
    public ReferenceType referenceType { get; set; }
    public string referenceObjectTypeId { get; set; }
    public ReferenceObjectType referenceObjectType { get; set; }
}

public class AssetResponseReferencedObject
{
    public string workspaceId { get; set; }
    public string globalId { get; set; }
    public string id { get; set; }
    public string label { get; set; }
    public string objectKey { get; set; }
    public Avatar avatar { get; set; }
    public ObjectType objectType { get; set; }
    public DateTime created { get; set; }
    public DateTime updated { get; set; }
    public bool hasAvatar { get; set; }
    public long timestamp { get; set; }
    public Links _links { get; set; }
    public string name { get; set; }
}

public class AssetResponseReferenceObjectType
{
    public string workspaceId { get; set; }
    public string globalId { get; set; }
    public string id { get; set; }
    public string name { get; set; }
    public Icon icon { get; set; }
    public int position { get; set; }
    public DateTime created { get; set; }
    public DateTime updated { get; set; }
    public int objectCount { get; set; }
    public string objectSchemaId { get; set; }
    public bool inherited { get; set; }
    public bool abstractObjectType { get; set; }
    public bool parentObjectTypeInherited { get; set; }
}

public class AssetResponseReferenceType
{
    public string workspaceId { get; set; }
    public string globalId { get; set; }
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public string color { get; set; }
    public string url16 { get; set; }
    public bool removable { get; set; }
}


