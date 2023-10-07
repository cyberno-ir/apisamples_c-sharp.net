using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace CSharpLib
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(@" ______ ____    ____ .______    _______. ______      .__   __.   ______   
/      |\   \  /   / |   _  \  |   ____||   _  \     |  \ |  |  /  __  \  
|,----'  \   \/   /  |  |_)  | |  |__   |  |_)  |    |   \|  | |  |  |  | 
| |       \_    _/   |   _  <  |   __|  |      /     |. ` |  | |  |  |  | 
| `----.    |  |     |  |_)  | |  |____ |  |\  \----.|  |\   | |  `--'  | 
\______|    |__|     |______/  |_______|| _| `._____||__| \__|  \______/  ");
            string identifier = "", password = "", serveraddress = "";
            string file_path = "";
            Console.Write("Please insert API server address [Default=https://multiscannerdemo.cyberno.ir/]: ");
            serveraddress = Console.ReadLine();
            if(serveraddress == "")
                serveraddress = "https://multiscannerdemo.cyberno.ir/";
            if (serveraddress.EndsWith("/") == false)
                serveraddress += "/";
            Console.Write("Please insert identifier (email): ");
            identifier = Console.ReadLine();
            Console.Write("Please insert your password: ");
            password = Console.ReadLine();
            CyUtils cyutils = new CyUtils(serveraddress);
            JObject params1 = new JObject();
            params1.Add("email", identifier);
            params1.Add("password", password);
            JObject login_response = cyutils.call_with_json_input("user/login", params1);
            if (login_response.SelectToken("success").ToObject<bool>() == true)
                Console.Write("You are logged in successfully.\r\n");
            else
            {
                Console.Write(cyutils.get_error(login_response));
                Console.ReadLine();
                return;
            }
            string apikey = login_response.SelectToken("data").ToObject<string>();
            int index;
            JObject scan_init_response;
            Console.Clear();
            Console.WriteLine("Please select scan mode:");
            Console.WriteLine("1- Scan local folder");
            Console.WriteLine("2- Scan file");
            Console.Write("Enter Number=");
            index = int.Parse(Console.ReadLine());
            if (index == 1)
            {

                // Prompt user to enter file paths
                string paths;
                string sentenceTwo = (" ");
                Console.WriteLine("Please enter the paths of file to scan (with spaces): ");
                paths = Console.ReadLine();
                sentenceTwo = paths;
                string[] file_path_array = sentenceTwo.Split(' ');


                // Prompt user to enter antivirus names
                string avs;
                string avsSentenceTwo = (" ");
                Console.WriteLine("Enter the name of the selected antivirus (with spaces): ");
                avs = Console.ReadLine();
                sentenceTwo = paths;
                string[] avs_array = avsSentenceTwo.Split(' ');

            
                // Make Json item
                JObject scan_item_json = new JObject();
                scan_item_json.Add("token", apikey);
                scan_item_json.Add("paths", new JArray(file_path_array));
                scan_item_json.Add("avs", new JArray(avs_array));

                scan_init_response = cyutils.call_with_json_input("scan/init", scan_item_json);
                if (scan_init_response.SelectToken("success").ToObject<bool>() == true)
                    Console.WriteLine(scan_init_response);
                else
                {
                    Console.Write(cyutils.get_error(scan_init_response));
                    Console.ReadLine();
                    return;
                }
            }
            else
            {
                // Initialize scan
                Console.Write("Please enter the path of file to scan: ");
                file_path = Console.ReadLine();
                string file_name = Path.GetFileName(file_path);

                Console.Write("Enter the name of the selected antivirus (with spaces): ");
                string avs = Console.ReadLine();

                JObject params2 = new JObject();
                params2.Add("file", file_name);
                params2.Add("token", apikey);
                params2.Add("avs", avs);
                scan_init_response = cyutils.call_with_form_input("scan/multiscanner/init", params2, "file", file_path);
                if (scan_init_response.SelectToken("success").ToObject<bool>() == true)
                    Console.WriteLine(scan_init_response);
                else
                {
                    Console.Write(cyutils.get_error(scan_init_response));
                    Console.ReadLine();
                    return;
                }
            }

            string guid = scan_init_response["guid"].ToString();
            if (scan_init_response["success"].ToObject<bool>() == true)
            {
                // Get scan response
                int password_protected = (int)scan_init_response["password_protected"].Count();
                // Check if password-protected
                if (password_protected > 0)
                {
                    JObject password_item_json = new JObject();
                    for (int i = 0; i < password_protected; i++)
                    {
                        string password_file;
                        Console.Write("|Enter the Password file -> " + scan_init_response["password_protected"][i] + " |: ");
                        password_file = Console.ReadLine();
                        password_item_json["password"] = password_file;
                        password_item_json["token"] = apikey;
                        password_item_json["path"] = scan_init_response["password_protected"][i];
                        cyutils.call_with_json_input("scan/extract/" + guid, password_item_json);
                    }
                }
            }
            // Start scan
            Console.WriteLine("=========  Start Scan ===========");
            JObject scan_json = new JObject();
            scan_json["token"] = apikey;
            JObject scan_response = cyutils.call_with_json_input("scan/start/" + guid, scan_json);
            if (scan_response["success"].ToObject<bool>() == true)
            {
                bool is_finished = false;
                while (!is_finished)
                {
                    Console.WriteLine("Waiting for result...");
                    JObject input_json = new JObject();
                    input_json["token"] = apikey;
                    JObject scan_result_response = cyutils.call_with_json_input("scan/result/" + guid, input_json);
                    try
                    {
                        if (scan_result_response["data"]["finished_at"].Value<int>() != 0)
                        {
                            is_finished = true;
                            Console.WriteLine(scan_result_response["data"]);
                        }
                    }

                    catch (Exception)
                    {
                        Thread.Sleep(5000);
                        continue;
                    }

                }
            }
            else
            {
                Console.WriteLine(cyutils.get_error(scan_response));
            }
            return;
        }
    }
}
