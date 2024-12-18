using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using UnityEngine;

///<summary>
///  Class that contains all per instance information related to a character.
///  Base stats are derived from <see cref="CharacterBase"/>.
///</summary>
public class Character
{
	public Character(CharacterBase characterBase)
	{
		Base = characterBase;

		CurrentHealth = MaxHealth;
		CurrentStamina = MaxStamina;
	}
	public event Action<Character> OnDeath;

	// stat/gain loss events are stored separately for future damage effects systems to use easily
	public event Action<Character, DamageEvent> OnDamage;
	public event Action<Character, int> OnHeal;

	public event Action<Character, int> OnStaminaDeplete;
	public event Action<Character, int> OnStaminaGain;

	public event Action<Character, ActionAnimType> OnAnimationStart;
	public event Action<Character> OnActionPerformed;
	public event Action<Character> OnTurnEnd;

	public CharacterBase Base { get; private set; }

	// shorthand variable to expose the character base's actions
	public ReadOnlyCollection<CombatAction> CombatActions => Base.CombatActions;

	public bool IsEnemy => Base.IsEnemy;

	public string Name => Base.Name;

	// primarily used to ensure multi-targeting attacks don't target dead characters
	///<summary>
	///  Whether the character should be ignored when targeting for an action, or in the turn order.
	///</summary>
	public bool IsDead { get; private set; } = false;

	#region Stats
	public List<StatBoost> ActiveStatBoosts { get; private set; } = new List<StatBoost>();
	public int CurrentHealth { get; private set; }
	public int CurrentStamina { get; private set; }

	public int MaxHealth => Mathf.FloorToInt(ApplyStatBoosts(Base.BaseHealth, StatTypes.MaxHealth));

	public int MaxStamina => Mathf.FloorToInt(ApplyStatBoosts(Base.BaseStamina, StatTypes.MaxStamina));

	public int Speed => Mathf.FloorToInt(ApplyStatBoosts(Base.BaseSpeed, StatTypes.Speed));

	public int Defense => Mathf.FloorToInt(ApplyStatBoosts(Base.BaseDefense, StatTypes.Defense));

	// attack is used as a MULTIPLIER to damage dealt.
	// this is slightly weird with the way stat boosts are handled,
	// as if you wanted to give a 20% attack boost, you'd actually ADD 0.2 to this.
	// sorry!! -cate (we tried to find a way around this but gave up)
	private float _attack = 1;
	///<returns>A multiplier for how much damage should be dealt by the character.</returns>
	public float Attack => ApplyStatBoosts(_attack, StatTypes.Attack);

	///<param name="baseValue">The base value the stat to be modified</param>
	///<param name="statType">The type of stat to look for in modifiers.</param>
	///<summary>
	///  Applies all the relevant <see cref="StatBoost"/>s to a given stat.
	///</summary>
	///<returns>
	///  The modified value after all boosts have been applied.
	///  Though most stats are stored as <see cref="int"/>,
	///  we can be more specific by returning <see cref="float"/> in case we need one.
	///</returns>
	public float ApplyStatBoosts(float baseValue, StatTypes statType)
	{
		// store a temporary version of the stat for additive boosts
		float modifiedStat = baseValue;

		// find boosts that match our stat we're trying to increase
		List<StatBoost> applicableBoosts = ActiveStatBoosts.Where(sb => sb.StatIncrease == statType).ToList();

		// used for multiplicative boosts to ensure they are not compounding (10% + 10% = 20% instead of 21%)
		float finalMultiplier = 1;

		foreach (StatBoost boost in applicableBoosts)
		{
			if (boost.Type == StatBoostTypes.Additive)
			{
				modifiedStat += boost.Amount;
			}
			else if (boost.Type == StatBoostTypes.Multiplicative)
			{
				finalMultiplier += boost.Amount;
			}
		}

		modifiedStat *= finalMultiplier;

		return modifiedStat;
	}
	#endregion Stats

	#region Stat Accessors
	///<param name="damage">
	///  <para>The amount of damage to deal to the character.</para>
	///  <para>Negative values are ignored.</para>
	///</param>
	///<summary>
	///  <para>
	///    Decreases the character's health.
	///    The amount the health stat is decreased by is not directly solely determined by <paramref name="damage"/>,
	///    but instead uses a formula that takes the character's defense stat into account.
	///  </para>
	///  <para>Also broadcasts and event with the amount of health was gained.</para>
	///</summary>
	public void Damage(int damage, bool ignoreDefense = false)
	{
		if (damage <= 0) return;

		// calculate defense damage reduction (uses a formula that can be found here: https://riskofrain2.fandom.com/wiki/Armor
		float defenseMultiplier = 1;
		if (!ignoreDefense)
		{
			defenseMultiplier = 1 - (Defense / (100 + Mathf.Abs(Defense)));
		}
    
		int appliedDamage = Mathf.FloorToInt(damage * defenseMultiplier);

		CurrentHealth -= appliedDamage;

		if (CurrentHealth <= 0)
		{
			CurrentHealth = 0;
			IsDead = true;
			OnDeath?.Invoke(this);
		}
		OnDamage?.Invoke(this, new DamageEvent(appliedDamage, damage));

	}

	///<param name="heal">
	///  <para>The amount of health to add to the character.</para>
	///  <para>Negative values are ignored.</para>
	///</param>
	///<summary>
	///  <para>Increases the character's health stat by <paramref name="heal"/>.
	///  The health stat will not go below 0.</para>
	///  <para>Also broadcasts and event with the amount of health was gained.</para>
	///</summary>
	public void Heal(int heal)
	{
		if (heal <= 0 || CurrentHealth == MaxHealth) return;

		int appliedHeal = heal;

		// do not allow overhealing
		if (CurrentHealth + heal > MaxHealth)
		{
			appliedHeal = MaxHealth - CurrentHealth;
			CurrentHealth = MaxHealth;
		}
		else
		{
			CurrentHealth += heal;
		}

		OnHeal?.Invoke(this, appliedHeal);
	}

	///<param name="stamina">
	///  <para>The amount of stamina to remove from the character.</para>
	///  <para>Negative values are ignored.</para>
	///</param>
	///<summary>
	///  <para>Decreases the character's stamina stat by <paramref name="stamina"/>.
	///  The stamina stat will not go below 0.</para>
	///  <para>Also broadcasts and event with the amount of stamina was lost.</para>
	///</summary>
	public void DepleteStamina(int stamina)
	{
		// TODO: Add check to prevent stamina from becoming negative
		if (stamina <= 0) return;

		CurrentStamina -= stamina;

		OnStaminaDeplete?.Invoke(this, stamina);
	}

	///<param name="stamina">
	///  <para>The amount of stamina to give to the character.</para>
	///  <para>Negative values are ignored.</para>
	///</param>
	///<summary>
	///  <para>Increments the character's stamina stat by <paramref name="stamina"/>.
	///  The stamina stat will not go above <see cref="MaxStamina"/>.</para>
	///  <para>Also broadcasts and event with the amount of stamina was gained.</para>
	///</summary>
	public void GainStamina(int stamina)
	{
		if (stamina <= 0) return;

		int gainedStamina = stamina;

		// do not allow stamina gain past the maximum
		if (CurrentStamina + stamina > MaxStamina)
		{
			gainedStamina = MaxStamina - CurrentStamina;
			CurrentStamina = MaxStamina;
		}
		else
		{
			CurrentStamina += stamina;
		}

		OnStaminaGain?.Invoke(this, gainedStamina);
	}
	#endregion

	public void StartAnimation(ActionAnimType animType)
	{
		OnAnimationStart?.Invoke(this, animType);
	}
	public void PerformAction()
	{
		OnActionPerformed?.Invoke(this);
	}
	public void EndTurn()
	{
		OnTurnEnd?.Invoke(this);
	}

	public override string ToString()
	{
		return Name;
	}

	#region Status Effects
	private List<StatusEffect> _statuses = new();

	public void ApplyStatus(StatusEffect statusEffect)
	{
		StatusEffect existingMatch = _statuses.FirstOrDefault(s => s.Equals(statusEffect));
		if (existingMatch != null)
		{
			if (statusEffect.Duration > existingMatch.Duration)
			{
				existingMatch.SetDuration(statusEffect.Duration);
			}
		}
		else
		{
			_statuses.Add(statusEffect);
			foreach (StatBoost boost in statusEffect.StatBoosts)
			{
				ActiveStatBoosts.Add(boost);
			}
		}
	}

	public void UpdateStatuses()
	{
		foreach (StatusEffect statusEffect in _statuses)
		{
			foreach (StatusEffectProc proc in statusEffect.Procs)
			{
				proc.Proc(this);
			}
			statusEffect.DecreaseDuration();
		}

		List<StatusEffect> expiredStatuses = _statuses.Where(s => s.Duration <= 0).ToList();
		foreach (StatusEffect statusEffect in expiredStatuses)
		{
			foreach (StatBoost boost in statusEffect.StatBoosts)
			{
				StatBoost existingMatch = ActiveStatBoosts.FirstOrDefault(s => s.Equals(boost));
				if (existingMatch != null)
				{
					//use discard to void return value explicitly
					_ = ActiveStatBoosts.Remove(existingMatch);
				}
			}
		}
	}

	public void ClearStatuses()
	{
		_statuses.Clear();
		ActiveStatBoosts.Clear();
	}
	#endregion
}

///<summary>
///  Class containing information about an instance of damage.
///</summary>
public class DamageEvent
{
	///<param name="appliedDamage">Damage the target was dealt.</param>
	///<param name="damage">Damage before defense calculations.</param>
	public DamageEvent(int appliedDamage, int damage)
	{
		AppliedDamage = appliedDamage;
		Damage = damage;
	}

	///<returns>The amount of damage actually applied to a target.</returns>
	public int AppliedDamage { get; private set; }
	///<returns>The raw damage number before defense calculations.</returns>
	public int Damage { get; private set; }
}
