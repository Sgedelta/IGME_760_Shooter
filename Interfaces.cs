using Godot;
using System;


public interface IDamagable
{
    public int Health { get; }

    public void TakeDamage(int damage);

}
