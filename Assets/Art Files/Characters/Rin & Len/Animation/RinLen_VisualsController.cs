using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RinLen_VisualsController : MonoBehaviour
{
    [SerializeField]
	private SpriteRenderer charSprite;
    [SerializeField]
	private SpriteRenderer charSprite2;
    //[VFX]
    [SerializeField]
    private ParticleSystem Dash;
    [SerializeField]
    private ParticleSystem ultDash1;
    [SerializeField]
    private ParticleSystem ultDash2;
    [SerializeField]
    private ParticleSystem ultDash3;

    public void changeSortingLayerUP()
	{
		charSprite.sortingLayerName = "CharacterInUse";
		charSprite2.sortingLayerName = "CharacterInUse";
		print("Called. Current Layer: " + charSprite.sortingLayerName);
		print("Called. Current Layer: " + charSprite2.sortingLayerName);
	}
	public void changeSortingLayerDOWN()
	{
		charSprite.sortingLayerName = "Characters";
		charSprite2.sortingLayerName = "Characters";
		print("Called. Current Layer: " + charSprite.sortingLayerName);
		print("Called. Current Layer: " + charSprite2.sortingLayerName);
	}
}
