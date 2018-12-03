using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class add : MonoBehaviour {
    
    public float mytime = 0;
    public GameObject gobj;
    public int count = 0;

    // Use this for initialization
    void Start () {
        Instantiate(this.gobj);
        count++;
    }
	
	// Update is called once per frame
	void Update () {
        mytime += Time.deltaTime;
        //Debug.Log(mytime);
        mypoisson pp = new mypoisson();

        if (pp.is_new_product((mytime/10000) , (float)count )) {
            Instantiate(this.gobj);
            count++;
            Debug.Log(count);
        }
    }
}

public class mypoisson{

    //VAL會與柏松分布的FUNC所計算出來的值比較大小
    float val;

    //讓VAL得到一個(>0.0) && (<1.0) 的值
    private void getnext()
    {
        for (val = Random.Range(0.0f,1.0f); val == 0;) val = Random.Range(0.0f, 1.0f);
    }

    //計算柏松分布在k=times時的機率
    private float pro_of_poi(float lamda, float times)
    {

        float pro = Mathf.Exp((-1) * lamda);

        for (int i = 0; i < times; i++)
        {
            pro /= (i + 1);
            pro *= lamda;
        }
        return pro;
    }

    //回傳在k=times時，隨機變數 >= P(x=times)
    public bool is_new_product(float lamda, float times)
    {
        getnext();

        return val <= pro_of_poi(lamda, times);
    }
}
