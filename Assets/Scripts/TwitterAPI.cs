using UnityEngine;

using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Collections;

using MiniJSON;

public class TwitterAPI : MonoBehaviour
{
    public string oauthConsumerKey = "";
    public string oauthConsumerSecret = "";
    public string oauthToken = "";
    public string oauthTokenSecret = "";

    private string oauthNonce = "";
    private string oauthTimeStamp = "";

    public static TwitterAPI instance = null;

    // Use this for initialization
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogError("More then one instance of TwitterAPI: " + this.transform.name);
        }
    }

    // Use of MINI JSON http://forum.unity3d.com/threads/35484-MiniJSON-script-for-parsing-JSON-data
    private List<TwitterData> ParseResultsFromSearchTwitter(string jsonResults)
    {
        List<TwitterData> twitterDataList = new List<TwitterData>();

        IDictionary search = (IDictionary)Json.Deserialize(jsonResults);
        IList tweets = (IList)search["statuses"];
        foreach (IDictionary tweet in tweets)
        {
            IDictionary userInfo = tweet["user"] as IDictionary;

            TwitterData twitterData = new TwitterData();
            twitterData.tweetText = tweet["text"] as string;
            twitterData.screenName = userInfo["screen_name"] as string;
            twitterData.retweetCount = (Int64)tweet["retweet_count"];
            twitterData.profileImageUrl = userInfo["profile_image_url"] as string;

            twitterDataList.Add(twitterData);
        }

        return twitterDataList;
    }

    public void SearchTwitter(string keywords, Action<List<TwitterData>> callback)
    {
        // Override the nounce and timestamp here if troubleshooting with Twitter's OAuth Tool
        oauthNonce = Convert.ToBase64String(new ASCIIEncoding().GetBytes(DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture)));
        TimeSpan _timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
        oauthTimeStamp = Convert.ToInt64(_timeSpan.TotalSeconds).ToString(CultureInfo.InvariantCulture);

        StartCoroutine(SearchTwitter_Coroutine(keywords, callback));
    }

    private IEnumerator SearchTwitter_Coroutine(string keywords, Action<List<TwitterData>> callback)
    {
        // Fix up hashes to be webfriendly
        keywords = Uri.EscapeDataString(keywords);

        string twitterUrl = "https://api.twitter.com/1.1/search/tweets.json";

        SortedDictionary<string, string> twitterParamsDictionary = new SortedDictionary<string, string>
        {
            {"q", keywords},
            {"count", "100"},
            {"result_type", "popular"},
        };

        string signature = CreateSignature(twitterUrl, twitterParamsDictionary);
        Debug.Log("OAuth Signature: " + signature);

        string authHeaderParam = CreateAuthorizationHeaderParameter(signature, this.oauthTimeStamp);
        Debug.Log("Auth Header: " + authHeaderParam);

        Dictionary<string, string> headers = new Dictionary<string, string>();
        headers["Authorization"] = authHeaderParam;

        string twitterParams = ParamDictionaryToString(twitterParamsDictionary);

        WWW query = new WWW(twitterUrl + "?" + twitterParams, null, headers);
        yield return query;

        callback(ParseResultsFromSearchTwitter(query.text));
    }

    // Taken from http://www.i-avington.com/Posts/Post/making-a-twitter-oauth-api-call-using-c
    private string CreateSignature(string url, SortedDictionary<string, string> searchParamsDictionary)
    {
        //string builder will be used to append all the key value pairs
        StringBuilder signatureBaseStringBuilder = new StringBuilder();
        signatureBaseStringBuilder.Append("GET&");
        signatureBaseStringBuilder.Append(Uri.EscapeDataString(url));
        signatureBaseStringBuilder.Append("&");

        //the key value pairs have to be sorted by encoded key
        SortedDictionary<string, string> urlParamsDictionary = new SortedDictionary<string, string>
                             {
                                 {"oauth_version", "1.0"},
                                 {"oauth_consumer_key", this.oauthConsumerKey},
                                 {"oauth_nonce", this.oauthNonce},
                                 {"oauth_signature_method", "HMAC-SHA1"},
                                 {"oauth_timestamp", this.oauthTimeStamp},
                                 {"oauth_token", this.oauthToken}
                             };

        foreach (KeyValuePair<string, string> keyValuePair in searchParamsDictionary)
        {
            urlParamsDictionary.Add(keyValuePair.Key, keyValuePair.Value);
        }

        signatureBaseStringBuilder.Append(Uri.EscapeDataString(ParamDictionaryToString(urlParamsDictionary)));
        string signatureBaseString = signatureBaseStringBuilder.ToString();

        Debug.Log("Signature Base String: " + signatureBaseString);

        //generation the signature key the hash will use
        string signatureKey =
            Uri.EscapeDataString(this.oauthConsumerSecret) + "&" +
            Uri.EscapeDataString(this.oauthTokenSecret);

        HMACSHA1 hmacsha1 = new HMACSHA1(
            new ASCIIEncoding().GetBytes(signatureKey));

        //hash the values
        string signatureString = Convert.ToBase64String(
            hmacsha1.ComputeHash(
                new ASCIIEncoding().GetBytes(signatureBaseString)));

        return signatureString;
    }

    private string CreateAuthorizationHeaderParameter(string signature, string timeStamp)
    {
        string authorizationHeaderParams = String.Empty;
        authorizationHeaderParams += "OAuth ";

        authorizationHeaderParams += "oauth_consumer_key="
                                     + "\"" + Uri.EscapeDataString(this.oauthConsumerKey) + "\", ";

        authorizationHeaderParams += "oauth_nonce=" + "\"" +
                                     Uri.EscapeDataString(this.oauthNonce) + "\", ";

        authorizationHeaderParams += "oauth_signature=" + "\""
                                     + Uri.EscapeDataString(signature) + "\", ";

        authorizationHeaderParams += "oauth_signature_method=" + "\"" +
            Uri.EscapeDataString("HMAC-SHA1") +
            "\", ";

        authorizationHeaderParams += "oauth_timestamp=" + "\"" +
                                     Uri.EscapeDataString(timeStamp) + "\", ";

        authorizationHeaderParams += "oauth_token=" + "\"" +
                                     Uri.EscapeDataString(this.oauthToken) + "\", ";

        authorizationHeaderParams += "oauth_version=" + "\"" +
                                     Uri.EscapeDataString("1.0") + "\"";
        return authorizationHeaderParams;
    }

    private string ParamDictionaryToString(IDictionary<string, string> paramsDictionary)
    {
        StringBuilder dictionaryStringBuilder = new StringBuilder();
        foreach (KeyValuePair<string, string> keyValuePair in paramsDictionary)
        {
            //append a = between the key and the value and a & after the value
            dictionaryStringBuilder.Append(string.Format("{0}={1}&", keyValuePair.Key, keyValuePair.Value));
        }

        string paramString = dictionaryStringBuilder.ToString().Substring(0, dictionaryStringBuilder.Length - 3);
        return paramString;
    }
}
