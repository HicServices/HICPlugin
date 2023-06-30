using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Serialization;
using SCIStorePlugin.Data;

namespace SCIStorePlugin.Repositories;

public class CombinedReportXmlDeserializer
{
    private readonly XmlSerializer _serializer;

    public CombinedReportXmlDeserializer()
    {
        _serializer = new XmlSerializer(typeof (CombinedReportData));
    }

    public CombinedReportData DeserializeFromZipEntry(ZipArchiveEntry entry, string fileLocation)
    {
        using (var stream = entry.Open())
        {
            try
            {
                return _serializer.Deserialize(stream) as CombinedReportData;
            }
            catch (Exception)
            {
                // we have failed so will fall through to attempt below
            }
        }

        // Not putting this into the above catch as the Deflate stream can't be rewound, and would rather open a new stream for safety
        // Exception might be due to invalid characters, attempt to replace them then deserialize again
        using (var stream = entry.Open())
        {
            return RetryDeserializationAfterCharacterReplacement(stream, fileLocation);
        }
    }

    public CombinedReportData DeserializeFromXmlString(string xml)
    {
        using var reader = new StringReader(xml);
        return _serializer.Deserialize(reader) as CombinedReportData;
    }

    public string RemoveInvalidCharactersFromStream(Stream stream)
    {
        // The first three were found in Fife Haematology data, they are PCL escape codes
        var characterSubstitutions = new Dictionary<string, string>
        {
            {"&#x1B;(s3B", "[b]"}, // begin bold
            {"&#x1B;(s0B", "[/b]"}, // end bold
            {"&#x1B;(s", "[unknown|x1B;(s]"}, // looks like truncation, in original file it looked like a truncation of 'end bold',
            {"&#x1B;", ""} // basic escape sequence, if this remains on its own then get rid of it
        };

        using var reader = new StreamReader(stream);
        var xmlString = reader.ReadToEnd();
        return characterSubstitutions.Aggregate(xmlString, (current, value) => current.Replace(value.Key, value.Value));
    }

    private CombinedReportData RetryDeserializationAfterCharacterReplacement(Stream stream, string fileLocation)
    {
        CombinedReportData report;

        try
        {
            var xmlString = RemoveInvalidCharactersFromStream(stream);

            var xmlSerialiser = new XmlSerializer(typeof(CombinedReportData));
            var reader = new StringReader(xmlString);
            report = xmlSerialiser.Deserialize(reader) as CombinedReportData;
        }
        catch (Exception e)
        {
            throw new Exception($"Error deserializing report, even after replacing invalid characters:{fileLocation}", e);
        }

        return report;
    }
}