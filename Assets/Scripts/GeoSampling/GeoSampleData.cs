using System;

namespace GeoSampling
{
    [Serializable]
    public class RockComposition
    {
        public float SiO2;
        public float Al2O3;
        public float MnO;
        public float CaO;
        public float P2O3;
        public float TiO2;
        public float FeO;
        public float MgO;
        public float K2O;
        public float Other;
        public string rockName;
    }

    [Serializable]
    public class GeoSampleData
    {
        public string sampleId;
        public string sampleType;
        public string location;
        public string imagePath;
        public string voiceTranscription;
        public RockComposition rockComposition;

        public static GeoSampleData CreateNew(string location, string imagePath, string voiceTranscription)
        {
            return new GeoSampleData
            {
                sampleId = Guid.NewGuid().ToString(),
                sampleType = "Rock Sample", // Default type
                location = location,
                imagePath = imagePath,
                voiceTranscription = voiceTranscription,
                rockComposition = new RockComposition()
            };
        }
    }
} 