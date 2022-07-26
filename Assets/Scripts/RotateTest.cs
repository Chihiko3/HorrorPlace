using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateTest : MonoBehaviour
{
    void Update()
    {
        float randomNum = Random.Range(0f, 0.5f);

        this.gameObject.transform.Rotate(randomNum, -randomNum, -randomNum);
    }
}
