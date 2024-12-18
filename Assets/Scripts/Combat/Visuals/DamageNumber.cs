using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamageNumber : MonoBehaviour
{
	[SerializeField]
	private TextMeshPro _text;
	public TextMeshPro Text => _text;

	[SerializeField]
	private float _lifetime;

	[SerializeField]
	private float _ascendSpeed;

	private void OnEnable()
	{
		Destroy(gameObject, _lifetime);
	}

	private void Update()
	{
		transform.position = new Vector2(transform.position.x, transform.position.y + _ascendSpeed * Time.deltaTime);
	}
}
