using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace CSharpLib
{
    public class CyUtils
    {
        private const string USER_AGENT = "Cyberno-API-Sample-CSharp";

        private string server_address;
        JObject unknownerror_respone_json;

        public CyUtils(string server_address)
        {
            this.server_address = server_address;

            this.unknownerror_respone_json = new JObject();
            this.unknownerror_respone_json.Add("error_code", 900);
            this.unknownerror_respone_json.Add("success", false);
        }

        public string get_sha256(string file_path) {
            using (var sha256_var = SHA256.Create())
            {
                using (var stream = File.OpenRead(file_path))
                {
                    byte[] file_sha256 = sha256_var.ComputeHash(stream);
                    StringBuilder Hex = new StringBuilder(file_sha256.Length * 2);
                    foreach (Byte b in file_sha256)
                        Hex.AppendFormat("{0:x2}", b);
                    return Hex.ToString();
                }
            }
        }

        public String get_error(JObject return_value)
        {
            String error = "Error!\n";
            if (return_value.ContainsKey("error_code"))
                error += ("Error code: " + return_value.SelectToken("error_code").ToObject<string>() + "\n");
            if (return_value.ContainsKey("error_desc"))
                error += ("Error description: " + return_value.SelectToken("error_desc").ToObject<string>() + "\n");
            return error;
        }

        public JObject call_with_json_input(string api, JObject json_input) {
            HttpWebRequest HttpWebRequest = (HttpWebRequest)WebRequest.Create(this.server_address + api);
            HttpWebRequest.ContentType = "application/json";
            HttpWebRequest.Method = "POST";
            HttpWebRequest.UserAgent = USER_AGENT;
            try
            {
                using (var streamWriter = new StreamWriter(HttpWebRequest.GetRequestStream()))
                {
                    string parsedContent = json_input.ToString();
                    streamWriter.Write(parsedContent);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
            }
            catch (Exception)
            {
                return unknownerror_respone_json;
            }
            string result;
            try
            {
                HttpWebResponse httpResponse = (HttpWebResponse)HttpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    result = streamReader.ReadToEnd();
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                    return this.unknownerror_respone_json;
                using (var stream = ex.Response.GetResponseStream())
                {
                    using (var reader = new StreamReader(stream))
                        result = reader.ReadToEnd();
                }
            }
            try
            {
                JObject srtr;
                srtr = JObject.Parse(result);
                return srtr;
            }
            catch (Exception)
            {
                return unknownerror_respone_json;
            }
        }

        public JObject call_with_form_input(string api, JObject data_input, string file_param_name, string file_path) {
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(this.server_address + api);
            wr.ContentType = "multipart/form-data; boundary=" + boundary;
            wr.Method = "POST";
            wr.KeepAlive = true;
            wr.Headers.Add("UserAgent", USER_AGENT);
            wr.Credentials = System.Net.CredentialCache.DefaultCredentials;
            Stream rs = wr.GetRequestStream();
            string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
            JToken data_input_mover = data_input.First;
            for (int i = 0; i < data_input.Count; i++)
            {
                rs.Write(boundarybytes, 0, boundarybytes.Length);
                string key = ((JProperty)data_input_mover).Name;
                string value = data_input_mover.First.Value<string>();
                string formitem = String.Format(formdataTemplate, key, value);
                byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                rs.Write(formitembytes, 0, formitembytes.Length);
                data_input_mover = data_input_mover.Next;
            }

            rs.Write(boundarybytes, 0, boundarybytes.Length);
            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n\r\n";
            string header = String.Format(headerTemplate, file_param_name, "file");
            byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
            rs.Write(headerbytes, 0, headerbytes.Length);
            FileStream fs = new FileStream(file_path, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[4096];
            int bytesRead = 0;
            do
            {
                bytesRead = fs.Read(buffer, 0, buffer.Length);
                if (bytesRead != 0)
                    rs.Write(buffer, 0, bytesRead);
            } while (bytesRead != 0);
            fs.Close();

            byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            rs.Write(trailer, 0, trailer.Length);
            rs.Close();

            string result;
            try
            {
                HttpWebResponse httpResponse = (HttpWebResponse)wr.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    result = streamReader.ReadToEnd();
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                    return unknownerror_respone_json;
                using (var stream = ex.Response.GetResponseStream())
                {
                    using (var reader = new StreamReader(stream))
                        result = reader.ReadToEnd();
                }
            }
            try
            {
                JObject srtr;
                srtr = JObject.Parse(result);
                return srtr;
            }
            catch (Exception)
            {
                return unknownerror_respone_json;
            }
        }
    }
}
