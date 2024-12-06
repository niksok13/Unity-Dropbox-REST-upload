using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace DropboxApi
{
    public class ResponseDropboxRefreshToken
    {
        public string access_token;
        public int expires_in;
    }
    
    public class ResponseDropboxUploadFile
    {
        public string name;
        public string path_lower;
        public string path_display;
        public string id;
        public string client_modified;
        public string server_modified;
        public string rev;
        public int size;
        public bool is_downloadable;
        public string content_hash;
    }
    
    public class RestDropbox
    {
        public RestDropbox(string refreshToken, string clientId, string clientSecret)
        {
            RefreshToken = refreshToken;
            ClientId = clientId;
            ClientSecret = clientSecret;
        }

        private string ClientId { get; }
        private string RefreshToken { get; }
        private string ClientSecret { get; }

        private const string UploadUrl = "https://content.dropboxapi.com/2/files/upload";
        private const string TokenRefreshUrl = "https://api.dropbox.com/oauth2/token";

        private const string TimeSpanFormatString = @"O";
        
        private static readonly string DefaultDate = DateTime.MinValue.ToString(TimeSpanFormatString);


        private async Task<string> GetToken()
        {
            var expireString = EditorPrefs.GetString("dropbox_token_expiry", DefaultDate);
            var expireTime = DateTime.ParseExact(expireString, TimeSpanFormatString, null);

            if (expireTime > DateTime.Now)
                return EditorPrefs.GetString("dropbox_token","");

            Debug.Log("Refreshing dropbox access token");
            
            var form = new WWWForm();
            form.AddField("grant_type", "refresh_token");
            form.AddField("refresh_token", RefreshToken);
            form.AddField("client_id", ClientId);
            form.AddField("client_secret", ClientSecret);
            
            var request = UnityWebRequest.Post(TokenRefreshUrl, form);
            request.SendWebRequest();
            
            while (!request.isDone) await Task.Delay(1000);
            if (request.error != null)
            {
                request.Dispose();
                throw new Exception(request.error + request.downloadHandler.text);
            }

            var response = JsonUtility.FromJson<ResponseDropboxRefreshToken>(request.downloadHandler.text);
            if (response == null)
            {
                request.Dispose();
                throw new Exception("parse error");
            }
            request.Dispose();
            
            expireTime = DateTime.Now.AddSeconds(response.expires_in);
            expireString = expireTime.ToString(TimeSpanFormatString);
            EditorPrefs.SetString("dropbox_token_expiry", expireString);
            EditorPrefs.SetString("dropbox_token", response.access_token);
            return response.access_token;
        }

        public async Task Upload(byte[] file, string fileName)
        {
            var token = await GetToken();
            Debug.Log($"Uploading {fileName} to Dropbox");
            var request = UnityWebRequest.PostWwwForm(UploadUrl, "POST");
            request.SetRequestHeader("Authorization", $"Bearer {token}");
            request.SetRequestHeader("Dropbox-API-Arg", $"{{\"autorename\": true, \"path\": \"/Builds/{fileName}\"}}");
            request.uploadHandler = new UploadHandlerRaw(file);
            request.SendWebRequest();

            while (!request.isDone) await Task.Delay(1000);

            if (request.error != null)
            {
                request.Dispose();
                throw new Exception(request.error + request.downloadHandler.text);
            }
            var response = JsonUtility.FromJson<ResponseDropboxUploadFile>(request.downloadHandler.text);
            if (response == null)
            {
                request.Dispose();
                throw new Exception("parse error");
            }
            request.Dispose();
        }
    }
}
