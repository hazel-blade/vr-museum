using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SwordCollected : MonoBehaviour
{
    [SerializeField]
    private int numberOfSwordItems;

    private int numberOfSwordItemsCollected = 0;

    public UnityEvent ItemsCollectedDoSomething;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddSwordItem()
    {
        numberOfSwordItemsCollected++;
        CheckCollectAll();
    }
    
    public void RemoveSwordItem()
    {
        numberOfSwordItemsCollected--;
    }

    private void CheckCollectAll()
    {
        if(numberOfSwordItemsCollected == numberOfSwordItems)
        {
            ItemsCollectedDoSomething.Invoke();
        }
    }
}
