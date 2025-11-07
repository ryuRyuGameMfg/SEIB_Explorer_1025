using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Legend : MonoBehaviour
{

    public GameObject[] WoodyPFTs_ = new GameObject[10];  //木のprefab。どのprefabを割り当てるかは、インスペクターを介して設定する。
    public GameObject[] tree_3d_ = new GameObject[5];    //個木にassigneされるprefab
    public Transform parent;

    // Start is called before the first frame update
    void Start()
    {
        /*
        tree_3d_[1] = Instantiate(WoodyPFTs_[2], parent, true) as GameObject;
        tree_3d_[1].transform.position = new Vector3(10f, 0f, 10f);
        tree_3d_[1].transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

        tree_3d_[2] = Instantiate(WoodyPFTs_[1], parent, true) as GameObject;
        tree_3d_[2].transform.position = new Vector3(5f, 0f, 0f);
        tree_3d_[2].transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

        tree_3d_[3] = Instantiate(WoodyPFTs_[0], parent, true) as GameObject;
        tree_3d_[3].transform.position = new Vector3(10f, 0f, 0f);
        tree_3d_[3].transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        */
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetMouseButtonDown(0))
        {
            SceneManager.LoadScene("Main");
        }

    }
}
