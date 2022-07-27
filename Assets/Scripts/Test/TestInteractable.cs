using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestInteractable : InteractableTemplate
{
    public override void OnFocus()
    {
        print("LOOKING AT " + gameObject.name);
    }

    public override void OnInteract()
    {
        print("INTERACTED WITH " + gameObject.name);
    }

    public override void OnLoseFocus()
    {
        print("STOP LOOKING AT " + gameObject.name);
    }
}
