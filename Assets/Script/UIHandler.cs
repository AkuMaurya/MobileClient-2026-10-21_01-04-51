using System.Collections;
using UnityEngine;

public class UIHandler : MonoBehaviour
{
    [SerializeField]
    TCPCarClient tcpCarClient;
    public GameObject controllerUI;
    public GameObject connectionUI;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(EnableController());
    }
    IEnumerator EnableController()
    {
        yield return new WaitUntil(() => tcpCarClient.IsConnected()); 
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
