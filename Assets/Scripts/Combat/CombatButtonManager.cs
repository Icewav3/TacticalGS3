using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

///<summary>
///  Class that bridges <see cref="CharacterActionSelector"/> with the UI and player input.
///</summary>
public class CombatButtonManager : MonoBehaviour
{
	[SerializeField]
	private List<CombatButton> _buttons = new();
	private List<CombatAction> _actions = new();
	///<summary>
	///  <para>Event that is broadcast when an action as been selected.</para>
	///  <para>Passes itself and the selected <see cref="CombatAction"/>.</para>
	///</summary>
	public event Action<CombatButtonManager, CombatAction> OnActionSelected;

	///<summary>
	///  Fills in and activates the UI involved with selecting a <see cref="CombatAction"/>.
	///</summary>
	public void Populate(List<CombatAction> combatActions)
	{
		foreach (CombatButton button in _buttons)
		{
			button.gameObject.SetActive(false);
		}
		_actions = combatActions;
		// TODO: Fill out UI buttons with info for their respective actions
		for (int i = 0; i < _actions.Count; i++)
		{
			_buttons[i].gameObject.SetActive(true);
			_buttons[i].Text.text = _actions[i].DisplayName;
		}
	}
	///<summary>
	///  Cleans up the combat UI at the end of a turn.
	///</summary>
	public void DeInit()
	{
		// TODO: Clean up and disable UI buttons to prepare for a new batch
		foreach (CombatButton button in _buttons)
		{
			button.gameObject.SetActive(false);
		}
	}

	public void SelectAction(int actionIndex)
	{
		OnActionSelected?.Invoke(this, _actions[actionIndex]);
	}
}