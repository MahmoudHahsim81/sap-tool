using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;   // System.Web.Extensions
using System.Windows.Controls;
using Microsoft.Win32;


public static class LicenseManager
{
    // ===== إعداد عام للـ TLS =====
    static LicenseManager()
    {
        try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }
    }

    // ===== التخزين =====
    private static readonly string StoreDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "HashimSapTool");
    private static readonly string TokenPath = Path.Combine(StoreDir, "license.jwt");

    // ===== الإندبوينت العام =====
    public static string ActivationUrl = "http://localhost:3000/activate";

    // 🔑 المفتاح العام بصيغة XML (Modulus/Exponent Base64) — بدّله بمفتاحك الحقيقي
    public static string PublicRsaXml =
        "<RSAKeyValue><Modulus>pcpN/QMES9xivzSiaNrkww1rFqn9eZ1+zw7T0RGWe0+oqxSMOgoY+0VUag1N0azeayaTxyMOFc80zH+p+0jTlgADDHTp0QMMqTUNunF5lVPrJqB+26IuQ5p9oJ49Hn+g8D4T+Gh/DQzv9UKCZFoeGVAqxZ23MMPdngDC7BPyD4nKKt7cojV8ztZIQ03LTj5xCJ6S8iYt+2MW4Gt2yE52K+hirAu9xSb/1bGPlVR6Yjjk39Wm9eky4rNtaw2zBluUAtVhqb/bXrajk4siJM8hlEvBjO61vPzmDVQ604ftLAgXk88K4dxYkQifCDdb7dEtfRYzcWHp+Q51uscOljgIzw==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

    // ======================= API =======================

    public static string MachineId()
    {
        try
        {
            using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography"))
            {
                object v = rk != null ? rk.GetValue("MachineGuid") : null;
                if (v != null) return v.ToString();
            }
        }
        catch { }
        return Environment.MachineName;
    }

    public static bool TryLoadValidToken(out IDictionary<string, object> claims, out string reason)
    {
        claims = null; reason = null;
        if (!File.Exists(TokenPath)) { reason = "No token."; return false; }
        try
        {
            string jwt = File.ReadAllText(TokenPath, Encoding.UTF8).Trim();
            return ValidateJwt(jwt, out claims, out reason);
        }
        catch (Exception ex) { reason = ex.Message; return false; }
    }

    public static async Task<Tuple<bool, string>> ActivateOnlineAsync(string licenseKey)
    {
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        string json = serializer.Serialize(new
        {
            key = (licenseKey ?? "").Trim(),
            machineId = MachineId(),
            product = "HashimSapTool"
        });

        HttpClientHandler handler = new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        using (HttpClient http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) })
        using (StringContent content = new StringContent(json, Encoding.UTF8, "application/json"))
        {
            HttpResponseMessage resp;
            try { resp = await http.PostAsync(ActivationUrl, content); }
            catch (Exception ex) { return Tuple.Create(false, "Network error: " + ex.Message); }

            if (!resp.IsSuccessStatusCode)
                return Tuple.Create(false, "Server: " + (int)resp.StatusCode);

            string body = await resp.Content.ReadAsStringAsync();

            // متوقع {"token":"<JWT>"}
            string token = ExtractTokenFromJson(body);
            if (string.IsNullOrWhiteSpace(token))
                return Tuple.Create(false, "No token returned.");

            string whyNot;
            if (!ValidateJwt(token, out _, out whyNot))
                return Tuple.Create(false, "Invalid token: " + whyNot);

            try { Directory.CreateDirectory(StoreDir); } catch { }
            File.WriteAllText(TokenPath, token, Encoding.UTF8);
            return Tuple.Create(true, "ok");
        }
    }

    public static Tuple<bool, HashSet<string>> GetFeatures(IDictionary<string, object> claims)
    {
        if (claims == null) return Tuple.Create(false, new HashSet<string>());
        object f; HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (claims.TryGetValue("features", out f) && f != null)
        {
            string s = Convert.ToString(f);
            foreach (string part in s.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                set.Add(part.Trim());
        }
        return Tuple.Create(true, set);
    }

    // ======================= JWT Validation (RS256) =======================

    public static bool ValidateJwt(string jwt, out IDictionary<string, object> claims, out string reason)
    {
        claims = null; reason = null;
        try
        {
            string[] parts = jwt.Split('.');
            if (parts.Length != 3) { reason = "Bad token format."; return false; }

            byte[] headerBytes = Base64UrlDecode(parts[0]);
            byte[] payloadBytes = Base64UrlDecode(parts[1]);
            byte[] sigBytes = Base64UrlDecode(parts[2]);

            string headerJson = Encoding.UTF8.GetString(headerBytes);
            string payloadJson = Encoding.UTF8.GetString(payloadBytes);

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            Dictionary<string, object> header = serializer.Deserialize<Dictionary<string, object>>(headerJson) ?? new Dictionary<string, object>();
            string alg = header.ContainsKey("alg") ? Convert.ToString(header["alg"]) : null;
            if (!string.Equals(alg, "RS256", StringComparison.OrdinalIgnoreCase))
            { reason = "Unsupported alg."; return false; }

            // Verify signature over header.payload
            byte[] signingInput = Encoding.UTF8.GetBytes(parts[0] + "." + parts[1]);
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(PublicRsaXml);
            bool ok = rsa.VerifyData(signingInput, new SHA256CryptoServiceProvider(), sigBytes);
            if (!ok) { reason = "Bad signature."; return false; }

            Dictionary<string, object> dict = serializer.Deserialize<Dictionary<string, object>>(payloadJson)
                                               ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            // exp (Unix seconds)
            if (dict.ContainsKey("exp"))
            {
                long expUnix;
                double expD;
                DateTime expUtc;
                if (long.TryParse(Convert.ToString(dict["exp"]), out expUnix))
                    expUtc = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
                else if (double.TryParse(Convert.ToString(dict["exp"]), out expD))
                    expUtc = DateTimeOffset.FromUnixTimeSeconds((long)expD).UtcDateTime;
                else expUtc = DateTime.MaxValue;

                if (DateTime.UtcNow > expUtc) { reason = "Token expired."; return false; }
            }

            // nbf (optional)
            if (dict.ContainsKey("nbf"))
            {
                long nbf;
                if (long.TryParse(Convert.ToString(dict["nbf"]), out nbf))
                {
                    DateTime nbfUtc = DateTimeOffset.FromUnixTimeSeconds(nbf).UtcDateTime;
                    if (DateTime.UtcNow < nbfUtc) { reason = "Token not active yet."; return false; }
                }
            }

            // machineId
            object midObj;
            if (!dict.TryGetValue("machineId", out midObj) ||
                !string.Equals(Convert.ToString(midObj), MachineId(), StringComparison.OrdinalIgnoreCase))
            { reason = "Machine mismatch."; return false; }

            claims = dict;
            return true;
        }
        catch (Exception ex) { reason = ex.Message; return false; }
    }

    // ======================= Helpers =======================

    private static string ExtractTokenFromJson(string json)
    {
        try
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            Dictionary<string, object> dict = serializer.Deserialize<Dictionary<string, object>>(json);
            if (dict != null && dict.ContainsKey("token")) return Convert.ToString(dict["token"]);
        }
        catch { }
        // fallback بسيط لو الاستجابة مختلفة: token="<...>"
        const string key = "\"token\"";
        int i = json.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (i >= 0)
        {
            int colon = json.IndexOf(':', i);
            int q1 = json.IndexOf('"', colon + 1);
            int q2 = json.IndexOf('"', q1 + 1);
            if (q1 > 0 && q2 > q1) return json.Substring(q1 + 1, q2 - q1 - 1);
        }
        return null;
    }

    private static byte[] Base64UrlDecode(string input)
    {
        string s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
    // ===== إضافة: قراءة التوكن من التخزين =====
    private static string ReadTokenSafely()
    {
        try
        {
            if (File.Exists(TokenPath))
                return File.ReadAllText(TokenPath, Encoding.UTF8).Trim();
        }
        catch { }
        return null;
    }

    // ===== إضافة: استخراج features من claims =====
    private static HashSet<string> ParseFeatures(IDictionary<string, object> claims)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (claims == null) return set;
        object f;
        if (claims.TryGetValue("features", out f) && f != null)
        {
            string s = Convert.ToString(f);
            foreach (var part in s.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                set.Add(part.Trim());
        }
        return set;
    }

    // ===== إضافة: فحص محلي سريع للميزة =====
    public static bool HasFeatureLocal(string feature, out string reason)
    {
        reason = null;
        IDictionary<string, object> claims;
        if (!TryLoadValidToken(out claims, out reason)) return false;
        var feats = ParseFeatures(claims);
        if (!feats.Contains(feature))
        {
            reason = $"Feature '{feature}' not granted.";
            return false;
        }
        return true;
    }

    // ===== إضافة: URL التحقق الأونلاين =====
    private static string GetValidateUrl()
    {
        // لو غيرت المسارات لاحقًا عدّلها هنا
        if (ActivationUrl.EndsWith("/activate", StringComparison.OrdinalIgnoreCase))
            return ActivationUrl.Substring(0, ActivationUrl.Length - "/activate".Length) + "/validate";
        return ActivationUrl.Replace("/activate", "/validate");
    }

    // ===== إضافة: تحقق أونلاين من التوكن (revoked/JTI/expired) =====
    public static async Task<(bool ok, string reason)> ValidateTokenOnlineAsync()
    {
        try
        {
            if (!File.Exists(TokenPath))
                return (false, "No local token.");

            string jwt = File.ReadAllText(TokenPath, Encoding.UTF8).Trim();

            using (var http = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) })
            using (var content = new StringContent("{\"token\":\"" + jwt.Replace("\"", "\\\"") + "\"}", Encoding.UTF8, "application/json"))
            {
                var resp = await http.PostAsync("http://localhost:3000/validate", content);
                if (!resp.IsSuccessStatusCode)
                    return (false, "Server refused token: " + (int)resp.StatusCode);

                var body = await resp.Content.ReadAsStringAsync();
                // لو تحب تتأكد محليًا من machineId كمان:
                if (!ValidateJwt(jwt, out _, out var why))
                    return (false, "Local verify failed: " + why);
            }
            return (true, "ok");
        }
        catch (Exception ex)
        {
            // مهم: نمنع التشغيل لو مفيش نت أو حصل خطأ
            return (false, "Online validation required: " + ex.Message);
        }
    }

    /// <summary>
    /// يُستخدم عند بداية التشغيل: لازم يكون فيه توكن محلي + يتأكد أونلاين دلوقتي.
    /// </summary>
    public static async Task<(bool ok, string reason)> RequireOnlineValidTokenAsync()
    {
        try
        {
            if (!File.Exists(TokenPath))
                return (false, "No local token.");

            string jwt = File.ReadAllText(TokenPath, Encoding.UTF8).Trim();

            using (var http = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            })
            { Timeout = TimeSpan.FromSeconds(8) })
            {
                var payloadObj = new { token = jwt };
                var json = new JavaScriptSerializer().Serialize(payloadObj);
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                {
                    HttpResponseMessage resp;
                    try
                    {
                        // تأكد إن عندك هذا الثابت:
                        // public static string ValidationUrl = "http://localhost:3000/validate";
                        resp = await http.PostAsync(ValidationUrl, content);
                    }
                    catch (Exception ex)
                    {
                        // مهم: أي خطأ شبكة = فشل
                        return (false, "Network/Server error: " + ex.Message);
                    }

                    if (!resp.IsSuccessStatusCode)
                        return (false, "Server HTTP " + (int)resp.StatusCode);

                    string body = await resp.Content.ReadAsStringAsync();

                    // لا تستخدم target-typed new
                    var dict = new JavaScriptSerializer()
                        .Deserialize<Dictionary<string, object>>(body)
                        ?? new Dictionary<string, object>();

                    if (!dict.TryGetValue("ok", out var okObj) || !(okObj is bool ok) || !ok)
                    {
                        string reason = dict.ContainsKey("reason") ? Convert.ToString(dict["reason"]) : "invalid";
                        return (false, reason);
                    }

                    // (اختياري) تأكيد machineId
                    if (dict.TryGetValue("payload", out var pl) && pl is Dictionary<string, object> pld)
                    {
                        if (pld.TryGetValue("machineId", out var mid) &&
                            !string.Equals(Convert.ToString(mid), MachineId(), StringComparison.OrdinalIgnoreCase))
                            return (false, "machine_mismatch");
                    }

                    return (true, "ok");
                }
            }
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public static async Task<(bool ok, string reason)> CheckFeatureAsync(string feature, bool onlineCheck)
    {
        // 1) لازم يكون فيه توكن محفوظ
        if (!File.Exists(TokenPath))
            return (false, "No local token.");

        string jwt = File.ReadAllText(TokenPath, Encoding.UTF8).Trim();

        // 2) تحقق محلّي من التوقيع + الصلاحية + MachineId
        if (!ValidateJwt(jwt, out var claims, out var why))
            return (false, "Invalid local token: " + why);

        // 3) تحقق إن الميزة موجودة في claims.features
        var feats = GetFeatures(claims).Item2;
        if (!string.IsNullOrWhiteSpace(feature) && !feats.Contains(feature))
            return (false, $"Feature '{feature}' is not allowed.");

        // 4) لو مطلوب أونلاين: لازم السيرفر يقول OK (وإلا امنع)
        if (onlineCheck)
        {
            try
            {
                using (var http = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) })
                using (var content = new StringContent(
                    new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new { token = jwt }),
                    Encoding.UTF8, "application/json"))
                {
                    var resp = await http.PostAsync(
                        ActivationUrl.Replace("/activate", "/validate"), content);

                    if (!resp.IsSuccessStatusCode)
                        return (false, $"Server validate: {(int)resp.StatusCode}");

                    var body = await resp.Content.ReadAsStringAsync();
                    // نعتبر أي body فيه "ok":true نجاح
                    if (!body.Contains("\"ok\":true"))
                        return (false, "Server denied token.");
                }
            }
            catch (Exception ex)
            {
                // سياسة صارمة: بدون إنترنت ⇒ مرفوض
                return (false, "Online check failed: " + ex.Message);
            }
        }

        return (true, "ok");
    }
    // علشان نقدر نمسح التوكن عند الفشل
    public static bool HasLocalToken()
    {
        return File.Exists(TokenPath);
    }

    // يتحقق أونلاين فقط. لو السيرفر مش متاح/النت فاصل -> فشل
    
    public static string ValidationUrl = "http://localhost:3000/validate";

    public static void ClearToken()
    {
        try
        {
            if (File.Exists(TokenPath))
                File.Delete(TokenPath);
        }
        catch { }
    }
}


