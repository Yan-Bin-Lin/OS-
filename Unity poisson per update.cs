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

    //VAL�|�P�f�Q������FUNC�ҭp��X�Ӫ��Ȥ���j�p
    float val;

    //��VAL�o��@��(>0.0) && (<1.0) ����
    private void getnext()
    {
        for (val = Random.Range(0.0f,1.0f); val == 0;) val = Random.Range(0.0f, 1.0f);
    }

    //�p��f�Q�����bk=times�ɪ����v
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

    //�^�Ǧbk=times�ɡA�H���ܼ� >= P(x=times)
    public bool is_new_product(float lamda, float times)
    {
        getnext();

        return val <= pro_of_poi(lamda, times);
    }
}
