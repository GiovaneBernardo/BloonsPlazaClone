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
using System.Runtime.Remoting;

public class Enemy
{
    public int Health;
    public int Damage;
    public int Speed;
    public Vector2 Position;
    public float DelayToStartMoving;
    public UInt64 Uuid;
    public int CurrentWayPoint;
    public float Time;
    public Transform Transform;
    public DateTime CreationTime;
    public bool CanMove = false;

    public Enemy(int health, int damage, int speed, Vector2 position, float delayToStartMoving)
    {
        this.Health = health;
        this.Damage = damage;
        this.Speed = speed;
        this.Position = position;
        this.Uuid = 0;
        this.CurrentWayPoint = 0;
        this.Time = 0.0f;
        this.DelayToStartMoving = delayToStartMoving;
        this.CreationTime = DateTime.Now;
    }
}

public class WayPoint
{
    public Vector3 Position;

    public WayPoint(Vector3 position)
    {
        Position = position;
    }
}

public class Wave
{
    public int MaxTime = 0;
    public int EnemiesToSpawn = 0;
    public int EnemiesPerUpdate = 0;
    public float SecondsPerSpawn = 0;
    public int CurrentEnemiesCount = 0;
    public int TotalEnemiesSpawned = 0;
    public DateTime LastSpawnTime = DateTime.Now;

    public Wave(int maxTime, int enemiesToSpawn, int enemiesPerUpdate, float secondsPerSpawn)
    {
        this.MaxTime = maxTime;
        this.EnemiesToSpawn = enemiesToSpawn;
        this.EnemiesPerUpdate = enemiesPerUpdate;
        this.SecondsPerSpawn = secondsPerSpawn;
    }
}



public class EnemiesManager : Entity
{
    public static List<Enemy> _enemies = new List<Enemy>();
    public static List<WayPoint> _wayPoints = new List<WayPoint>();
    public static List<Wave> _runningWaves = new List<Wave>();
    public static float _wayPointHeight = 1.0f;
    public static int _currentWave = 0;
    public static bool _runningWave = true;

    public void OnStart()
    {
        _wayPoints.Add(new WayPoint(new Vector3(0.0f, _wayPointHeight, -10.0f * CameraScript._gridSize)));
        _wayPoints.Add(new WayPoint(new Vector3(0.0f, _wayPointHeight, 10.0f * CameraScript._gridSize)));
        _wayPoints.Add(new WayPoint(new Vector3(10.0f * CameraScript._gridSize, _wayPointHeight, 10.0f * CameraScript._gridSize)));

        _currentWave++;
        SpawnWave();
    }

    public void OnUpdate()
    {
        UpdateEnemies();
        if (_runningWave)
        {
            UpdateWaveSpawner();
        }


    }

    public static void UpdateEnemies()
    {
        //if (_enemies.Count == 0)
        //{
        //    _currentWave++;
        //    Console.WriteLine("Wave: " + _currentWave);
        //    SpawnWave();
        //}

        foreach (Wave wave in _runningWaves)
        {
            if (wave.TotalEnemiesSpawned >= wave.EnemiesToSpawn)
            {
                _currentWave++;
                SpawnWave();
                _runningWaves.Remove(wave);
            }
        }

        if(_runningWaves.Count == 0)
        {
            _currentWave++;
            SpawnWave();
        }

        if (_runningWave)
        {
            for (int i = 0; i < _enemies.Count; ++i)
            {
                Enemy enemy = _enemies[i];


                if (!enemy.CanMove)
                {
                    enemy.CanMove = (DateTime.Now - enemy.CreationTime).TotalMilliseconds > enemy.DelayToStartMoving * 100.0f;
                    if (!enemy.CanMove)
                        continue;
                } 

                Vector2 ba = new Vector2(_wayPoints[enemy.CurrentWayPoint + 1].Position.X, _wayPoints[enemy.CurrentWayPoint + 1].Position.Z) - enemy.Position;
                ba.X *= enemy.Time * Time.deltaTime;
                ba.Y *= enemy.Time * Time.deltaTime;
                enemy.Position = enemy.Position + ba;
                enemy.Time = Math.Min(enemy.Time + 0.001f, 1.0f);
                //Vector3 pos = new Vector3(enemy.Position.X, 1.0f, enemy.Position.Y);
                //InternalCalls.SetPosition(enemy.Uuid, ref pos);
                enemy.Transform.Translation = new Vector3(enemy.Position.X, 1.0f, enemy.Position.Y);
                if (enemy.Time >= 1.0f)
                {
                    enemy.CurrentWayPoint++;
                    enemy.Time = 0.0f;
                    if (enemy.CurrentWayPoint + 1 >= _wayPoints.Count)
                    {
                        Entity.FindEntityByName("CameraEntity").GetScript<CameraScript>().Health -= enemy.Damage;
                        if (Entity.FindEntityByName("CameraEntity").GetScript<CameraScript>().Health <= 0)
                        {
                            Entity.FindEntityByName("CameraEntity").GetScript<CameraScript>().Death();
                        }
                        RemoveEnemy(i);
                    }
                }
            }
        }
    }

    public static void SpawnWave()
    {
        int enemiesCount = Math.Max((int)Math.Pow(_currentWave + 1, 3), 10);
        _runningWaves.Add(new Wave(60, enemiesCount, _currentWave, 0.4f));
        //for (int i = 0; i < enemiesCount; ++i)
        //{
        //    AddEnemy(new Enemy(5, 1, 1, new Vector2(_wayPoints[0].Position.X, _wayPoints[0].Position.Z)));
        //}
    }

    public static void UpdateWaveSpawner()
    {
        for (int i = 0; i < _runningWaves.Count; ++i)
        {
                Wave wave = _runningWaves[i];
            bool timePassedToSpawnMoreEnemies = (DateTime.Now - wave.LastSpawnTime).TotalSeconds > wave.SecondsPerSpawn;
            if (timePassedToSpawnMoreEnemies)
            {


                wave.LastSpawnTime = DateTime.Now;
                if (wave.TotalEnemiesSpawned < wave.EnemiesToSpawn)
                {
                    for (int j = 0; j < wave.EnemiesPerUpdate; ++j)
                    {
                        AddEnemy(new Enemy(5, 1, 1, _wayPoints[0].Position.XZ, 0.5f * j));
                        wave.TotalEnemiesSpawned++;
                    }
                }
                else
                {
                    _runningWaves.Remove(_runningWaves[i]);
                }
            }
        }
    }

    public static void AddEnemy(Enemy enemy)
    {
        enemy.Uuid = Entity.Instantiate(new Entity(FindEntityByNameCall("EnemyInstantiable"))).Uuid;
        new Entity(enemy.Uuid).Name = "enemy" + _enemies.Count;
        enemy.Transform = new Entity(enemy.Uuid).GetComponent<Transform>();
        _enemies.Add(enemy);
    }

    public static void RemoveEnemy(int index)
    {
        new Entity(_enemies[index].Uuid).Delete();
        _enemies.Remove(_enemies[index]);
    }

    public static void DeleteAllEnemies()
    {
        for (int i = 0; i < _enemies.Count; ++i)
        {
            new Entity(_enemies[i].Uuid).Delete();
            _enemies.RemoveAt(i);
        }
    }

    public static void DeleteAllWaves()
    {
        for(int i = 0; i < _runningWaves.Count; ++i)
        {
            _runningWaves.RemoveAt(i);
        }
    }
}