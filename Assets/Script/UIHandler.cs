using System.Collections;
using TMPro;
using UnityEngine;

public class UIHandler : MonoBehaviour
{
    [SerializeField]
    TCPCarClient tcpCarClient;
    public GameObject controllerUI;
    public GameObject connectionUI;
    public TMP_Text debugText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(EnableController());
    }
    IEnumerator OnSearchStart(){
        debugText.text = "Searching for server...";
        yield return new WaitUntil(() => tcpCarClient.IsConnected()); 
        debugText.text = "Connected to server!";
    }
    IEnumerator EnableController()
    {
        yield return new WaitUntil(() => tcpCarClient.IsConnected()); 
        yield return new WaitForSeconds(1f); // Optional: Add a small delay to ensure the connection is stable
        controllerUI.SetActive(true);
        connectionUI.SetActive(false);
    }
    // Update is called once per frame
    void Update()
    {
        
    }

    public void Quit()
    {
        Application.Quit();
    }
}
