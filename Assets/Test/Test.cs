using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    public List<GameObject> gameObjects = new List<GameObject>();
    // Start is called before the first frame update
    void Start()
    {
        Material mat = gameObjects[1].GetComponent<MeshRenderer>().material;
        mat.SetColor("_Color", Color.yellow);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
