using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plaza;
using static Plaza.InternalCalls;
using static Plaza.Input;
using static Plaza.Screen;
using System.IO;
using System.Runtime.Remoting.Services;
using Microsoft.Win32.SafeHandles;
using System.Collections;
using System.Threading;

public class Tower
{
    public int Price;
    public int TowerRange;
    public int Cooldown;
    public int Damage;
    public int AttackAreaOfEffect;
    public Vector2 Position;
    public UInt64 Uuid;
    public DateTime LastAttackTime;

    public Tower(int price, int towerRange, int cooldown, int damage, int attackAreaOfEffect, Vector2 position)
    {
        Price = price;
        TowerRange = towerRange;
        Cooldown = cooldown;
        Damage = damage;
        AttackAreaOfEffect = attackAreaOfEffect;
        Position = position;
        LastAttackTime = DateTime.Now;
    }

    public void Update()
    {
        bool elapsedSufficentTimeSinceLastTowerAttack = (DateTime.Now - LastAttackTime).TotalMilliseconds > (float)this.Cooldown * 100.0f;
        if (!elapsedSufficentTimeSinceLastTowerAttack)
            return;
        LastAttackTime = DateTime.Now;

        for (int i = 0; i < EnemiesManager._enemies.Count; i++)
        {
            Enemy enemy = EnemiesManager._enemies[i];
            if (Vector2.Distance(enemy.Position, Position) <= TowerRange)
            {
                enemy.Health -= this.Damage;

                if (enemy.Health <= 0)
                {
                    EnemiesManager.RemoveEnemy(i);
                    CameraScript._coins += 1;
                }
                break;
            }
        }
    }
}

public class TowersManager : Entity
{
    private static Dictionary<int, Dictionary<int, Tower>> _towersDictionary = new Dictionary<int, Dictionary<int, Tower>>();
    private static int _towersIndex = 0;

    public static void AddTower(int x, int y, Tower newTower, Entity baseTower)
    {
        if (HasTower(x, y))
            return;

        if (!_towersDictionary.ContainsKey(x))
        {
            _towersDictionary[x] = new Dictionary<int, Tower>();
        }

        newTower.Uuid = new Entity(Instantiate(new Entity(baseTower.Uuid)).Uuid).Uuid;//Entity.Instantiate(new Entity(newTower.Uuid))).Uuid;
        new Entity(newTower.Uuid).Name = baseTower.Name + _towersIndex;
        _towersDictionary[x][y] = newTower;
        _towersIndex++;
        // Start the infinite coroutine
        //FindEntityByName("mageTower").Instantiate(FindEntityByName("mageTower"));
    }

    public static void RemoveTower(int x, int y)
    {
        if (!HasTower(x, y))
            return;

        new Entity(GetTower(x, y).Uuid).Delete();
        _towersDictionary[x][y] = null;
    }



    public static Tower GetTower(int x, int y)
    {
        return _towersDictionary[x][y];
    }

    public static bool HasTower(int x, int y)
    {
        return _towersDictionary.ContainsKey(x) && _towersDictionary[x].ContainsKey(y) && _towersDictionary[x][y] != null;
    }

    public static void ModifyTower(int x, int y, Tower modifiedTower)
    {
        if (HasTower(x, y))
            _towersDictionary[x][y] = modifiedTower;
    }

    public static void UpdateTowers()
    {
        foreach (var outerPair in _towersDictionary)
        {
            int outerKey = outerPair.Key;
            Dictionary<int, Tower> innerDictionary = outerPair.Value;

            foreach (var innerPair in innerDictionary)
            {
                int innerKey = innerPair.Key;
                if (innerPair.Value == null)
                    continue;

                Tower tower = innerPair.Value;

                tower.Update();
            }
        }
    }

    public static void DeleteAllTowers()
    {
        foreach (var outerPair in _towersDictionary)
        {
            int outerKey = outerPair.Key;
            Dictionary<int, Tower> innerDictionary = outerPair.Value;

            foreach (var innerPair in innerDictionary)
            {
                new Entity(innerPair.Value.Uuid).Delete();
                innerDictionary.Remove(innerPair.Key);
            }
        }
    }

    public void OnStart()
    {

    }

    public void OnUpdate()
    {
        UpdateTowers();
    }
}