extern alias sdk;

using sdk::Newtonsoft.Json.Linq;
using sdk::osu;
using sdk::osu.Constants;
using sdk::osu_common.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Core.Feature
{
    // TODO: refactor
    public static class HWIDSpoof
    {
        private static JObject profile = null;
        public static void Load()
        {
            JArray arr = null;
            if (File.Exists("jamal/profiles.json"))
                arr = JArray.Parse(File.ReadAllText("jamal/profiles.json"));
            if (arr == null) arr = new JArray();
            Utility.Log("0. No profile\n1. Generate and load new profile");
            for (int i = 0; i < arr.Count; i++) Utility.Log((i + 2) + ". " + arr[i].ToObject<JObject>()["name"]);
            int pfl = Utility.Choose("Select profile: ");
            if (pfl == 1)
            {
                profile = GenerateProfile();
                arr.Add(profile);
                Utility.Success("New profile name: " + profile["name"]);
                File.WriteAllText("jamal/profiles.json", arr.ToString());
            }
            else if (pfl > 1) profile = arr[pfl - 2].ToObject<JObject>();
            Utility.Log(profile == null ? "Not using identity spoof.\n" : "Identity spoof is enabled.\n");
        }
        /*private static string md5_some_shit(string text2)
        {
            string text3;
            string s = text2;
            byte[] array = CryptoHelper.utf8Encoding.GetBytes(s);
            MD5 md5Hasher = CryptoHelper.md5Hasher;
            lock (md5Hasher)
            {
                try
                {
                    array = CryptoHelper.md5Hasher.ComputeHash(array);
                }
                catch (Exception)
                {
                    text3 = "fail";
                    goto IL_114;
                }
            }
            char[] array2 = new char[array.Length * 2];
            for (int j = 0; j < array.Length; j++)
            {
                array[j].ToString("x2", CryptoHelper.nfi).CopyTo(0, array2, j * 2, 2);
            }
            string text4 = new string(array2);
            goto IL_116;
            IL_114:
            text4 = text3;
            IL_116:
            string value = text4;
            return value;
        }*/
        public static void UpdateHWID()
        {
            if (profile == null) return;
            //GameBase.UniqueId = md5_some_shit(profile["uninstall"].ToString());
            //GameBase.UniqueId2 = md5_some_shit(profile["disk"].ToString());
            //GameBase.UniqueCheck = md5_some_shit(GameBase.UniqueId + 8.ToString() + 512.ToString() + GameBase.UniqueId2);
            //GameBase.UniqueCheck.c = GameBase.UniqueId.c = GameBase.UniqueId2.c = 1;
            Utility.Debug("ClientHash: " + GameBase.ClientHash);
            GameBase.ClientHash = GenerateClientHash();
			Utility.Debug("=> " + GameBase.ClientHash);
            Utility.Success("HWID updated.");
        }
        public static void FixWebRequest(pWebRequest req)
        {
            if (profile == null) return;
            if (req.get_Url().Contains("selector.php"))
            {
                string iv = req.Parameters["iv"], key = Encoding.ASCII.GetString(Secrets.GetScoreSubmissionKey());
                if (!req.Parameters.ContainsKey("c1")) Utility.Warn("c1 not found.");
                else
                {
                    Utility.Debug("c1: " + req.Parameters["c1"]);
                    req.Parameters["c1"] =
                        Utility.Stringify(CryptoHelper.GetMd5ByteArrayString(profile["uninstall"].ToString()))
                            + "|" + Utility.Stringify(CryptoHelper.GetMd5ByteArrayString(profile["disk"].ToString()));
                    Utility.Debug("=> " + req.Parameters["c1"]);
                }
                if (!req.Parameters.ContainsKey("s")) Utility.Warn("s not found.");
                else
                {
                    Utility.Debug("s: " + req.Parameters["s"]);
                    req.Parameters["s"] = Utility.REncrypt(GameBase.ClientHash, key, iv);
                    Utility.Debug("=> " + req.Parameters["s"]);
                }
                Utility.Success("Web request hwid fixed.");
            }
        }
        private static string GenerateClientHash()
        {
            return CryptoHelper.GetMd5(OsuMain.get_FullPath())
                + ":" + profile["adapters"].ToString()
                + ":" + Utility.Stringify(CryptoHelper.GetMd5ByteArrayString(profile["adapters"].ToString()))
                + ":" + Utility.Stringify(CryptoHelper.GetMd5ByteArrayString(profile["uninstall"].ToString()))
                + ":" + Utility.Stringify(CryptoHelper.GetMd5ByteArrayString(profile["disk"].ToString())) + ":";
        }
        private static JObject GenerateProfile()
        {
            return new JObject(
                new JProperty("name", Utility.RandomHex(10)),
                new JProperty("disk", new Random().Next(1000000, 2000000000).ToString()),
                new JProperty("adapters", Utility.RandomHex(6).ToUpper() + "."),
                new JProperty("uninstall", Guid.NewGuid().ToString()));
        }
    }
}
