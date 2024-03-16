using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF.SkinDiseaseDevice.Utility
{
    public class ConfigSkinDeAI
    {
        public static string ApiKey = "a5feedbb88ad44d990b8659383a51506";
        public static string Url = "https://southeastasia.api.cognitive.microsoft.com/customvision/v3.0/Prediction/624660db-a671-40f9-b68b-14743dc606a7/classify/iterations/SkinconditionVer2/image";
        public static string ContentType = "application/octet-stream";
    }

    public class ConfigAgeAI
    {
        public static string ApiKey = "a5feedbb88ad44d990b8659383a51506";
        public static string Url = "https://southeastasia.api.cognitive.microsoft.com/customvision/v3.0/Prediction/d935c757-0eed-42cf-ad83-fed8492f12fa/classify/iterations/AgePrediction/image";
        public static string ContentType = "application/octet-stream";
    }

}