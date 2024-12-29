using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Reflection;
using ConstantContactApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        public string strConn = @"server=10.10.10.11\dev;user=MyRecDept;password=;database=";
        private readonly string _ApiKey = System.Guid.NewGuid().ToString();
        private readonly string _ClientSecret = "secret";
        private string _AppName = "MyRec";
        //private string _AppLogoUrl = "https://myrec.com/images/logo-360.png";
        //private const string _ConstantContactUrl = "https://api.cc.email/v3/";
        private const string _ConstantContactUrl = "https://authz.constantcontact.com/oauth2/default/v1/"; //New PKCE Workflow
        private string _redirectUrl = "https://secure.myrecdept.com/constant_contact.aspx?DepartmentID=&ActivityID=";

        public T MapToClass<T>(SqlDataReader reader) where T : class
        {
            T returnedObject = Activator.CreateInstance<T>();
            List<PropertyInfo> modelProperties = returnedObject.GetType().GetProperties().OrderBy(p => p.MetadataToken).ToList();
            for (int i = 0; i < modelProperties.Count; i++)
                modelProperties[i].SetValue(returnedObject, Convert.ChangeType(reader.GetValue(i), modelProperties[i].PropertyType), null);
            return returnedObject;
        }

        [TestMethod]
        public void TestAuthentication()
        {
            var request = new ConstantContactApi.ConstantContactApi();
            var authUrl = request.GetAuthenticationUrl("https://secure.myrecdept.com/constant_contact.aspx?DepartmentID=&ActivityID=", _ApiKey, _ConstantContactUrl);

            var result = !String.IsNullOrEmpty(authUrl);
        }

        [TestMethod]
        public void TestGetContactsCall()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
            var request = new ConstantContactApi.ConstantContactApi();
            var token = request.GetAccessToken(strConn, "1", _redirectUrl, _ApiKey, _ClientSecret);

            var contacts = request.GetContacts(token.Access_token, _ConstantContactUrl);
            var result = contacts.Count > 1;
        }

        [TestMethod]
        public void TestCreateContact()
        {
            

            var request = new ConstantContactApi.ConstantContactApi();
            var token = request.GetAccessTokenFromDatabase(strConn, "53800");

            var contact = new ConstantContactApi.Contact
            {
                first_name = "Derpy",
                last_name = "Hooves",
                email_address = new ConstantContactApi.EmailAddress
                {
                     address = "test4@derpy.com"                    
                },
                create_source = "Account"                
            };

            var response = request.CreateContact(token, contact, _ConstantContactUrl);
        }

        [TestMethod]
        public void TestCreateList()
        {
            var request = new ConstantContactApi.ConstantContactApi();
            var token = request.GetAccessTokenFromDatabase(strConn, "100");

            var contactList = new ContactList()
            {
                name = "test list " + DateTime.Now.ToLongTimeString(),
                description = "this is a test",
                favorite = false
            };

            var response = request.CreateList(token, contactList, _ConstantContactUrl);

            var des = new RestSharp.Deserializers.JsonDeserializer().Deserialize<ListResponse>(response);

            var list_id = des.list_id;
        }

        [TestMethod]
        public void TestCreateContacts()
        {          

            var request = new ConstantContactApi.ConstantContactApi();
            var token = request.GetAccessTokenFromDatabase(strConn, "100");       


            var listContacts = new List<Contact2>();

            var sql = @"SELECT EmailAddress, FirstName, CASE WHEN LastName LIKE '%[\u1E00-\u1EFF]%' THEN '' ELSE LastName END AS LastName ";
            sql += "from EmailBlastsRecipients where EmailBlastID = @EmailBlastID AND OptIn = 1 ORDER BY LastName";

            using (SqlConnection conn = new SqlConnection(strConn))
            {
                var contact = new ConstantContactApi.Contact2();
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@EmailBlastID", SqlDbType.Int).Value = 20411;
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            contact = MapToClass<ConstantContactApi.Contact2>(reader);
                            listContacts.Add(contact);
                        };
                    }; 
                };
                conn.Close();
            };


            var response = request.ImportContacts(token, listContacts, "test list " + DateTime.Now.ToLongTimeString(), "This is a test", _ConstantContactUrl);

            var result = (response.StatusCode == System.Net.HttpStatusCode.Created);
            //var response = request.ImportContactsTest(token, listContacts, "test list " + DateTime.Now.ToLongTimeString(), "This is a test", _ConstantContactUrl);

            //var test = response

            //if (response.StatusCode != System.Net.HttpStatusCode.Created)
            //{
                
            //}
        }
    }
}
