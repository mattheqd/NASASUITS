using System;

[Serializable]
public class GeoSampleData
{
    public string sampleId;
    public string sampleType;
    public string location;
    public string imagePath;
    public string voiceTranscription;

    public static GeoSampleData CreateNew(string location, string imagePath, string voiceTranscription)
    {
        return new GeoSampleData
        {
            sampleId = Guid.NewGuid().ToString(),
            sampleType = "Rock Sample", // Default type
            location = location,
            imagePath = imagePath,
            voiceTranscription = voiceTranscription
        };
    }
} 