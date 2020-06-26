using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using NDR.HL72FHIR.Helper;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace NDR.HL72FHIR
{
    class Start
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            //var host = BuildWebHost(args);
            //host.Run();


            // 1. Load from HL7 Load table and get HL7 Message
            DataTable dtHl7Load = LoadFromHL7Load();

            // 2. Loop through datatable and pass HL7message as parameter
            foreach (DataRow dr in dtHl7Load.Rows)
            {
                Patient patient = new Patient();
                Encounter encounter = new Encounter();
                Procedure procedure = new Procedure();

                Bundle bundle = await SimulatePostmanPost((string)dr["hl7message"], (string)dr["MSH_MessageStructure"]);

                foreach (var entry in bundle.Entry)
                {
                    switch (entry.Resource.ResourceType)
                    {
                        case ResourceType.Patient:
                            patient = entry.Resource as Hl7.Fhir.Model.Patient;
                            break;
                        case ResourceType.Encounter:
                            encounter = entry.Resource as Hl7.Fhir.Model.Encounter;
                            break;
                        case ResourceType.Procedure:
                            procedure = entry.Resource as Hl7.Fhir.Model.Procedure;
                            break;
                    }
                }

                // Patient resource identifier
                Patient patientFromPyro = GetPatientFromPyroAPI(patient.Identifier[0].Value);

                //Identifier id = new Identifier { ElementId = patientFromPyro.Id.ToString() };
                patient.Id = patientFromPyro.Id.ToString();
                encounter.Subject.Reference = "Patient/" + patientFromPyro.Id.ToString();
                procedure.Subject.Reference = "Patient/" + patientFromPyro.Id.ToString();

                bundle.Id = "bun1";
                // Post the Encounter
                // PostEncounter(bundle);
                PostEncounterOnly(encounter);

            }

        }

        public static IWebHost BuildWebHost(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .Build();

        private static DataTable LoadFromHL7Load()
        {
            string sql = "SELECT TOP 1 hl7message, MSH_MessageStructure FROM ADTmessages";

            DataTable dt = ConnectionStringHelper.GetDataFromSQLtable(sql);

            return dt;

        }

        public static async Task<Bundle> SimulatePostmanPost(string hl7MessageAsBody, string messageType)
        {
            Hl7.Fhir.Model.Bundle bundle = new Bundle();
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            // Request parameters
            queryString["code"] = "ba086e26-c6ad-4c87-819f-d9d49c080c7a";

            var uri = string.Empty;

            if (messageType != "ADT_A01")
            {
                return bundle;
            }
            else
            {
                uri = "http://localhost:2019/api/convert/hl7v2/ADT_A01.hbs";
                uri = "https://fhirconverterapi.azurewebsites.net/api/convert/hl7v2/ADT_A01.hbs?" + queryString;
            }

            // Request headers
            //  client.DefaultRequestHeaders.Add("Ocp -Apim-Subscription-Key", "ba086e26-c6ad-4c87-819f-d9d49c080c7a");
            // client.DefaultRequestHeaders.Add("Authorization", "{access token}");


            StringBuilder body = new StringBuilder(@"MSH|^~\&|7A3WPAS|108|NWIS|200|20200312204400||ADT^A08^ADT_A01|108569960688|P|2.5.1|||AL");
            body.AppendLine(Environment.NewLine);
            body.AppendLine(@"EVN|A08|20200312195358|||SYSDBA");
            body.AppendLine(Environment.NewLine);
            body.AppendLine(
                @"PID|1||3795624134^^^NHS^NH~E100023^^^108^PI||Davies^Edley^JOHN^^Mr||1926-08-11|M|||C/ Whitchurch Street^Bwlch^Carmarthenshire^^SA31 4WH^UK~||^PRN^PH^^^^^^Day~386339^PRN^PH^^^^^^Night~^PRS^CP|||9|Z|||||AZ|||||||20171022|Y||01|20200312195358");
            body.AppendLine(Environment.NewLine);
            body.AppendLine(@"PD1|||GOWER MEDICAL PRACTICE^^^^^^^^^W98045|G9905135^THOMAS^SE");
            body.AppendLine(Environment.NewLine);
            body.AppendLine(@"PV1|1|O|51180^Si-Th-Am-M^100002092^511^^^^^Radiotherapy Outpatients Department^7A3C4|F|||||C9630353^Bell^Emma^SHG^^Dr|800000|||||||C9630353^Bell^Emma^^^Dr|OA|M100371970^^^108^VN|||||||||||||||||||||||||20131024103000||||||102492214");
            body.AppendLine(Environment.NewLine);
            body.AppendLine(@"PV2||||||||||||Oncology||||||||||||25");

            string[] newBody = hl7MessageAsBody.Split("\r");

            // TODO: Update the Source HL7 Message
            body = body.Replace("^^^^^^", "^");

            StringBuilder result = new StringBuilder();
            foreach (var item in newBody)
            {
                result.AppendLine(@item);
                result.AppendLine(Environment.NewLine);
            }

            result = result.Replace("^^^^^^", "^");
            // hl7MessageAsBody = hl7MessageAsBody.Replace(Environment.NewLine, "\r");

            HttpResponseMessage response = await client.PostAsync(uri, new StringContent(result.ToString()));
            response.EnsureSuccessStatusCode();

            //using (HttpResponseMessage response = await client.PostAsync(uri, new StringContent(body))
            //{
            using (HttpContent content = response.Content)
            {
                string responseString = await content.ReadAsStringAsync();
                FhirJsonParser jsonParser = new FhirJsonParser();

                JObject o = JObject.Parse(responseString);
                // o.Remove("unusedSegments");
                //o.Remove("invalidAccess");

                // remove or extract 
                var extractedResource = o.GetValue("fhirResource");
                string extractedResourceAsString = extractedResource.ToString();
                extractedResourceAsString = extractedResourceAsString.Replace(@"fhirResource:", "");

                //Hl7.Fhir.Model.Resource resource = jsonParser.Parse<Hl7.Fhir.Model.Resource>(extractedResourceAsString);

                bundle = jsonParser.Parse<Hl7.Fhir.Model.Bundle>(extractedResourceAsString);
            }

            return bundle;
        }

        //{

        //    Patient patient = await GetPatientsHttpClient();


        //    // Amend the Encounter returned from comversion
        //    return patient;
        //}

        public static Patient GetPatientFromPyroAPI(string id)
        {
            var client = new FhirClient("https://pyrowebapi.azurewebsites.net/fhir"); // Live
            client = new FhirClient("http://localhost:8888/fhir");
            //  client.

            Bundle bundle = client.Search<Patient>(new string[]
            {
                $"identifier=" + id
            });

            return bundle.Entry.First().Resource as Patient;
        }

        public static async void PostEncounter(Bundle bundle)
        {
            var client = new FhirClient("https://pyrowebapi.azurewebsites.net/fhir"); // Live
            client = new FhirClient("http://localhost:8888/fhir");
            //  client.

            var returnValue = await client.executeAsync<Hl7.Fhir.Model.Bundle>(bundle, System.Net.HttpStatusCode.OK);

            //return new Bundle();
        }

        public static async void PostEncounterOnly(Encounter encounter)
        {
            var client = new HttpClient();
            // var client = new HttpClient("https://pyrowebapi.azurewebsites.net/fhir"); // Live
            var uri = "http://localhost:8888/fhir/Encounter";
            //  client.

            JObject o = JObject.Parse(encounter.ToJson());
            o.Remove("serviceType");
            //o.Remove("participant");
            //o.Remove("period");
            //o.Remove("location");
            o.Remove("id");

            string convertedEncounter = o.ToString();

            HttpResponseMessage response = await client.PostAsync(uri, new StringContent(convertedEncounter));
            response.EnsureSuccessStatusCode();


            //return new Bundle();
        }

    }
}

