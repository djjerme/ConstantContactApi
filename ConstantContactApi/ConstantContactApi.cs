using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using RestSharp;
using RestSharp.Authenticators;


namespace ConstantContactApi
{
    public class ConstantContactRestRequest : RestRequest
    {
        public ConstantContactRestRequest()
        {
            RequestFormat = DataFormat.Json;
            AddHeader("Content-Type", "application/xml");
        }
    }

    public partial class ConstantContactApi : ConstantContactRestRequest
    {

        public ConstantContactApi()
        {

        }

        public string GetAuthenticationUrl(string redirectUrl, string apiKey, string constantContactUrl)
        {
            var authUrl = new StringBuilder();
            authUrl.AppendFormat("{0}authorize?client_id={1}&scope=contact_data&response_type=code&redirect_uri={2}&pn=myrec", constantContactUrl, apiKey, redirectUrl); //new PKCE workflow

            return authUrl.ToString();
        }

        public AuthToken GetAccessToken(string departmentId, string authCode, string redirectUrl, string apiKey, string clientSecret)
        {
            var client = new RestClient
            {
                //BaseUrl = new Uri("https://idfed.constantcontact.com/as/token.oauth2")
                BaseUrl = new Uri("https://authz.constantcontact.com/oauth2/default/v1/token") //new PKCE Workflow
            };

            var request = new RestRequest(Method.POST)
                .AddParameter("code", authCode)
                .AddParameter("redirect_uri", redirectUrl)
                .AddParameter("grant_type", "authorization_code");

            client.Authenticator = new HttpBasicAuthenticator(apiKey, clientSecret);
            var response = client.Execute<AuthToken>(request);

            return response.Data;
        }

        public string GetAccessTokenFromDatabase(string strConn, string departmentId)
        {
            var token = "";
            var SQL = "SELECT TOP 1 AccessToken FROM ConstantContactTokens WITH (NOLOCK)";
                SQL += " WHERE DepartmentID = @DepartmentId";
            using (SqlConnection conn = new SqlConnection(strConn))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(SQL, conn))
                {
                    cmd.Parameters.Add("@DepartmentId", SqlDbType.Int).Value = departmentId;

                    SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleResult);
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            token = reader["AccessToken"].ToString();
                        }
                    }
                }
                conn.Close();
            }
            return token;
        }

        public void UpdateAccessToken(string strConn, string departmentId, AuthToken token)
        {

            var authToken = token.Access_token;
            var refreshToken = token.Refresh_token;

            using (SqlConnection conn = new SqlConnection(strConn))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "sproc_Update_Constant_Contact_Tokens";

                    cmd.Parameters.Add("@DepartmentID", SqlDbType.Int).Value = Int32.Parse(departmentId);
                    cmd.Parameters.Add("@AccessToken", SqlDbType.VarChar, 255).Value = authToken;
                    cmd.Parameters.Add("@RefreshToken", SqlDbType.VarChar, 255).Value = refreshToken;

                    SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleResult);
                    if (reader.HasRows)
                    {

                    }

                    conn.Close();
                }
            }


        }

        public List<Contact> GetContacts(string accessToken, string constantContactUrl)
        {
            var client = new RestClient
            {
                BaseUrl = new System.Uri(constantContactUrl)
            };

            var request = new RestRequest("contacts", Method.GET)
                .AddHeader("Authorization", "Bearer " + accessToken)
                .AddParameter("include_count", false);

            var response = client.Execute<List<Contact>>(request);
            return response.Data;
        }

        public IRestResponse CreateList(string accessToken, ContactList contactList, string constantContactUrl)
        {
            var client = new RestClient
            {
                BaseUrl = new Uri(constantContactUrl)
            };

            var request = new RestRequest("contact_lists", Method.POST)
                .AddHeader("Content-Type", "application/json")
                .AddHeader("Accept", "application/json")
                .AddHeader("Cache-Control", "no-cache")
                .AddHeader("Authorization", "Bearer " + accessToken)
                .AddJsonBody(contactList);            

            return client.Execute(request);
        }

        public IRestResponse CreateContact(string accessToken, Contact contact, string constantContactUrl)
        {
            var client = new RestClient
            {
                BaseUrl = new Uri(constantContactUrl)
            };

            var request = new RestRequest("contacts", Method.POST)
                .AddHeader("Content-Type", "application/json")
                .AddHeader("Accept", "application/json")
                .AddHeader("Cache-Control", "no-cache")
                .AddHeader("Authorization", "Bearer " + accessToken)
                .AddJsonBody(contact);

            return client.Execute(request);
        }

        public IRestResponse ImportContacts(string accessToken, List<Contact2> listContacts, string listName, string listDescription, string constantContactUrl)
        {
            var contactList = new ContactList()
            {
                name = listName,
                description = listDescription,
                favorite = false
            };

            var response = CreateList(accessToken, contactList, constantContactUrl);
            var content = response.Content;

            if (response.StatusCode != System.Net.HttpStatusCode.Created)
            {
                return response;
            }

            var des = new RestSharp.Deserializers.JsonDeserializer().Deserialize<ListResponse>(response);

            var list_id = des.list_id;

            if (list_id != null)
            {

                var contacts = new Contacts()
                {
                    list_ids = list_id,
                    import_data = listContacts
                };

                var client = new RestClient
                {
                    BaseUrl = new Uri(constantContactUrl)
                };

                var request = new RestRequest("activities/contacts_json_import", Method.POST)
                    .AddHeader("Content-Type", "application/json")
                    .AddHeader("Accept", "application/json")
                    .AddHeader("Cache-Control", "no-cache")
                    .AddHeader("Authorization", "Bearer " + accessToken)
                    .AddJsonBody(contacts);

                return client.Execute(request);
            }
            else return null;
        }


        public IRestRequest ImportContactsTest(string accessToken, List<Contact2> listContacts, string listName, string listDescription, string constantContactUrl)
        {
            var contactList = new ContactList()
            {
                name = listName,
                description = listDescription,
                favorite = false
            };

            var response = CreateList(accessToken, contactList, constantContactUrl);
            var content = response.Content;

            var des = new RestSharp.Deserializers.JsonDeserializer().Deserialize<ListResponse>(response);

            var list_id = des.list_id;

            if (list_id != null)
            {

                var contacts = new Contacts()
                {
                    list_ids = list_id,
                    import_data = listContacts
                };

                var client = new RestClient
                {
                    BaseUrl = new Uri(constantContactUrl)
                };

                var request = new RestRequest("activities/contacts_json_import", Method.POST)
                    .AddHeader("Content-Type", "application/json")
                    .AddHeader("Accept", "application/json")
                    .AddHeader("Cache-Control", "no-cache")
                    .AddHeader("Authorization", "Bearer " + accessToken)
                    .AddJsonBody(contacts);

                var result = client.Get(request);

                return request;

            }
            else return null;

        }
    }

    public class Contact
    {
        public string contact_id { get; set; }
        public EmailAddress email_address { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string create_source { get; set; }
    }

    public class Contact2
    {        
        public string email { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
    }

    public class Contacts
    {
        public List<Contact2> import_data { get; set; }
        public string list_ids { get; set; }
    }

    public class EmailAddress
    {
        public string address { get; set; }
    }

    public class AuthToken
    {
        public string Access_token { get; set; }
        public string Refresh_token { get; set; }
    }

    public class ContactList
    {
        public string name { get; set; }
        public bool favorite { get; set; }
        public string description { get; set; }
    }

    public class ListResponse
    {
        public string list_id { get; set; }
        public string name { get; set; }
    }

    

}