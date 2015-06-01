using System.Linq;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TwitterSearch : MonoBehaviour
{
    [SerializeField] // SerializeField attribute makes the field visible in the Editors Inspector
    private InputField _searchTextBox;
    [SerializeField]
    private RectTransform _tweetsPanel;
    [SerializeField]
    private GameObject _tweetPrefab;

    void Start()
    {
        TwitterAPI.instance.SearchTwitter("#unity3d", ResultsCallBackToLog);
    }

    void ResultsCallBackToLog(List<TwitterData> tweetList)
    {
        Debug.Log("===================================================="); // Writes to the logs
        foreach (TwitterData twitterData in tweetList)
        {
            Debug.Log("Tweet: " + twitterData.ToString());
        }
    }

    /// Public method to assign a button click
    public void Search()
    {
        string searchText = _searchTextBox.text;
        TwitterAPI.instance.SearchTwitter(searchText, RequestImagesCallback);
    }

    void RequestImagesCallback(List<TwitterData> tweetList)
    {
        RemoveAllTweets();

        foreach (TwitterData twitterData in tweetList.Take(30))
        {
            var tweet = GameObject.Instantiate(_tweetPrefab); // GameObject.Instantate spawns a new instance of a prefab

            tweet.GetComponentInChildren<Text>().text = twitterData.tweetText; // Get the Text component and set it's .text value

            var image = tweet.GetComponentInChildren<RawImage>(); // Get the RawImage component

            //StartCoroutine begins an async call
            StartCoroutine(GetProfileImage(twitterData.profileImageUrl, image)); // Download and assign the Profile Image to the RawImage

            // Become a child of the _tweetsPanel and reset the scale
            tweet.transform.SetParent(_tweetsPanel);
            tweet.transform.localScale = Vector3.one;
        }
    }

    private void RemoveAllTweets()
    {
        foreach (Transform child in _tweetsPanel.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }

    private IEnumerator GetProfileImage(string profileImageUrl, RawImage image)
    {
        WWW www = new WWW(profileImageUrl);
        yield return www; // Perform the HTTPGET request
        image.texture = www.texture; // We know it's a texture so assign it using the .texture property
    }
}
