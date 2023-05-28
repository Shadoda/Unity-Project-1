using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TcgEngine
{
    /// <summary>
    /// Resolve abilties, at the moment not in use as it was more convenient to add it directly to GameLogic
    /// </summary>

    public class ResolveQueue 
    {
        public UnityAction<AbilityData, Card, Card> onAbilityResolve;
        public UnityAction<AbilityTrigger, Card, Card> onSecretResolve;
        public UnityAction<Card, Card> onAttackResolve;
        public UnityAction<Card, Player> onAttackPlayerResolve;

        private Pool<AbilityQueueElement> ability_elem_pool = new Pool<AbilityQueueElement>();
        private Pool<SecretQueueElement> secret_elem_pool = new Pool<SecretQueueElement>();
        private Pool<AttackQueueElement> attack_elem_pool = new Pool<AttackQueueElement>();
        private Pool<CallbackQueueElement> callback_elem_pool = new Pool<CallbackQueueElement>();

        private Queue<AbilityQueueElement> ability_cast_queue = new Queue<AbilityQueueElement>();
        private Queue<SecretQueueElement> secret_queue = new Queue<SecretQueueElement>();
        private Queue<AttackQueueElement> attack_queue = new Queue<AttackQueueElement>();
        private Queue<CallbackQueueElement> callback_queue = new Queue<CallbackQueueElement>();
        private bool is_resolving = false;

        public void AddAbility(AbilityData ability, Card caster, Card triggerer)
        {
            if (ability != null && caster != null)
            {
                AbilityQueueElement elem = ability_elem_pool.Create();
                elem.caster = caster;
                elem.triggerer = triggerer;
                elem.ability = ability;
                ability_cast_queue.Enqueue(elem);
            }
        }

        public void AddAttack(Card attacker, Card target)
        {
            if (attacker != null && target != null)
            {
                AttackQueueElement elem = attack_elem_pool.Create();
                elem.attacker = attacker;
                elem.target = target;
                attack_queue.Enqueue(elem);
            }
        }

        public void AddAttack(Card attacker, Player target)
        {
            if (attacker != null && target != null)
            {
                AttackQueueElement elem = attack_elem_pool.Create();
                elem.attacker = attacker;
                elem.ptarget = target;
                attack_queue.Enqueue(elem);
            }
        }

        public void AddSecret(AbilityTrigger secret_trigger, Card secret, Card trigger, Card other)
        {
            if (secret != null && trigger != null && other != null)
            {
                SecretQueueElement elem = secret_elem_pool.Create();
                elem.secret_trigger = secret_trigger;
                elem.secret = secret;
                elem.triggerer = trigger;
                secret_queue.Enqueue(elem);
            }
        }

        public void AddCallback(Action callback)
        {
            if (callback != null)
            {
                CallbackQueueElement elem = callback_elem_pool.Create();
                elem.callback = callback;
                callback_queue.Enqueue(elem);
            }
        }

        public void Resolve()
        {
            if (ability_cast_queue.Count > 0)
            {
                //Resolve Ability
                AbilityQueueElement elem = ability_cast_queue.Dequeue();
                ability_elem_pool.Dispose(elem);
                onAbilityResolve?.Invoke(elem.ability, elem.caster, elem.triggerer);
            }
            else if (secret_queue.Count > 0)
            {
                //Resolve Secret
                SecretQueueElement elem = secret_queue.Dequeue();
                secret_elem_pool.Dispose(elem);
                onSecretResolve?.Invoke(elem.secret_trigger, elem.secret, elem.triggerer);
            }
            else if (attack_queue.Count > 0)
            {
                //Resolve Attack
                AttackQueueElement elem = attack_queue.Dequeue();
                attack_elem_pool.Dispose(elem);
                if (elem.ptarget != null)
                    onAttackPlayerResolve?.Invoke(elem.attacker, elem.ptarget);
                else
                    onAttackResolve?.Invoke(elem.attacker, elem.target);
            }
            else if (callback_queue.Count > 0)
            {
                CallbackQueueElement elem = callback_queue.Dequeue();
                callback_elem_pool.Dispose(elem);
                elem.callback.Invoke();
            }
        }

        public void ResolveAll()
        {
            if (is_resolving)
                return;

            is_resolving = true;
            while (CanResolve())
            {
                Resolve();
            }
            is_resolving = false;
        }

        public bool CanResolve()
        {
            return attack_queue.Count > 0 || ability_cast_queue.Count > 0 || secret_queue.Count > 0 || callback_queue.Count > 0;
        }

        public bool IsResolving()
        {
            return is_resolving;
        }

        public void Clear()
        {
            attack_elem_pool.DisposeAll();
            ability_elem_pool.DisposeAll();
            secret_elem_pool.DisposeAll();
            callback_elem_pool.DisposeAll();
            attack_queue.Clear();
            ability_cast_queue.Clear();
            secret_queue.Clear();
            callback_queue.Clear();
        }
    }

    public class AbilityQueueElement
    {
        public AbilityData ability;
        public Card caster;
        public Card triggerer;
    }

    public class AttackQueueElement
    {
        public Card attacker;
        public Card target;
        public Player ptarget;
    }

    public class SecretQueueElement
    {
        public AbilityTrigger secret_trigger;
        public Card secret;
        public Card triggerer;
    }

    public class CallbackQueueElement
    {
        public Action callback;
    }
}
