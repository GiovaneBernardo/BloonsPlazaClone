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

public class CameraScript : Entity
{
    public int Health = 100;
    public float speed = 100.0f;
    public static int _coins = 300;
    public static int _gridSize = 16;
    public Tower TowerInHand = null;
    public Entity TowerEntity = null;
    public Transform RangePreviewTransform = null;

    public void OnStart()
    {
        RangePreviewTransform = FindEntityByName("RangePreview").GetComponent<Transform>();
    }

    Vector3 ScreenToWorldPoint(Vector3 screenPos)
    {
        float fieldOfView = 60.0f;
        float aspect = Screen.Size.X / Screen.Size.Y;

        Vector2 relative = new Vector2(
             screenPos.X / Screen.Size.X - 0.5f,
             screenPos.Y / Screen.Size.Y - 0.5f
        );

        float verticalAngle = 0.5f * Mathf.Deg2Rad * fieldOfView;
        float worldHeight = 2f * (float)Math.Tan((float)verticalAngle);

        Vector3 worldUnits = new Vector3(relative.X * worldHeight, relative.Y * worldHeight, 0.0f);
        worldUnits.X *= aspect;
        worldUnits.Z = 1;

        // Rotate to match camera orientation.
        Vector3 direction = this.GetComponent<Transform>().Rotation * worldUnits;
        return direction;
    }

    Vector2 WorldPointToGrid(Vector3 point)
    {
        Vector2 grid = new Vector2((float)Math.Round(point.X / _gridSize), (float)Math.Round(point.Z / _gridSize));
        return grid;
        //return point.XZ;
    }

    public void OnUpdate()
    {
        Vector3 newTranslation = this.GetComponent<Transform>().Translation;

        if (Input.IsKeyDown(KeyCode.W))
            newTranslation.Z += speed * Time.deltaTime;
        if (Input.IsKeyDown(KeyCode.S))
            newTranslation.Z -= speed * Time.deltaTime;
        if (Input.IsKeyDown(KeyCode.A))
            newTranslation.X += speed * Time.deltaTime;
        if (Input.IsKeyDown(KeyCode.D))
            newTranslation.X -= speed * Time.deltaTime;

        Vector3 mousePos = new Vector3(Screen.Size.X - Cursor.X, Screen.Size.Y - Cursor.Y, 0.01f);
        Vector3 rayDirection = Vector3.Normalize(ScreenToWorldPoint(mousePos));
        Physics.RaycastHit hit = Physics.Raycast(newTranslation, rayDirection, 150000.0f);
        Vector3 hitPoint = hit.point;

        Vector2 grid = WorldPointToGrid(hitPoint);

        if (TowerInHand != null)
        {
            Vector3 newPos = new Vector3(grid.X * _gridSize, hit.point.Y, grid.Y * _gridSize);
            TowerEntity.GetComponent<Transform>().Translation = newPos;//hit.point;
            RangePreviewTransform.Translation = new Vector3(newPos.X, 1.0f, newPos.Z);

            if (Input.IsMouseDown(0) && _coins >= TowerInHand.Price)
            {
                TowerInHand.Position = new Vector2((int)grid.X * _gridSize, (int)grid.Y * _gridSize);
                TowersManager.AddTower((int)grid.X, (int)grid.Y, new Tower(TowerInHand.Price, TowerInHand.TowerRange, TowerInHand.Cooldown, TowerInHand.Damage, TowerInHand.AttackAreaOfEffect, TowerInHand.Position), TowerEntity);
                _coins -= TowerInHand.Price;

                TowerInHand = null;
                TowerEntity.GetComponent<Transform>().Translation = new Vector3(10000.0f, 10.0f, 1000.0f);
            }
        }

        if (Input.IsMouseDown(1))
        {
            if (TowerEntity != null)
                TowerEntity.GetComponent<Transform>().Translation = new Vector3(10000.0f, 10.0f, 1000.0f);
            TowersManager.RemoveTower((int)grid.X, (int)grid.Y);
        }


        if (TowerInHand == null && TowersManager.HasTower((int)grid.X, (int)grid.Y))
        {
            RangePreviewTransform.Translation = new Vector3(grid.X * _gridSize, 1.0f, grid.Y * _gridSize);
        }
        else if (TowerInHand == null)
        {
            RangePreviewTransform.Translation = new Vector3(1000.0f, 1.0f, 1000.0f);
        }

        this.GetComponent<Transform>().Translation = newTranslation;

        HandleTowerSelection();
    }

    public void Death()
    {
        Console.WriteLine("A1");
        TowersManager.DeleteAllTowers();
        Console.WriteLine("A2");
        EnemiesManager.DeleteAllEnemies();
        Console.WriteLine("A3");
        EnemiesManager.DeleteAllWaves();
        Console.WriteLine("A4");
        _coins = 300;
        Health = 100;
    }

    public void HandleTowerSelection()
    {
        if (!Input.IsAnyKeyPressed())
            return;
        if (Input.IsKeyDown(KeyCode.D1))
        {
            RemoveTowerFromHand();
            TowerInHand = null;
        }
        else if (Input.IsKeyDown(KeyCode.D2)) // Mage Tower
        {
            RemoveTowerFromHand();
            TowerInHand = new Tower(500, 48, 1, 5, 2, new Vector2(-10000.0f, -10000.0f));
            TowerEntity = FindEntityByName("mageTower");
            RangePreviewTransform.Scale = new Vector3(48, 1.0f, 48);
        }
        else if (Input.IsKeyDown(KeyCode.D3)) // Archer Tower
        {
            RemoveTowerFromHand();
            TowerInHand = new Tower(150, 64, 1, 4, 1, new Vector2(-10000.0f, -10000.0f));
            TowerEntity = FindEntityByName("archerTower");
            RangePreviewTransform.Scale = new Vector3(64, 1.0f, 64);
        }
        else if (Input.IsKeyDown(KeyCode.D4)) // Cannon Tower
        {
            RemoveTowerFromHand();
            TowerInHand = new Tower(300, 32, 3, 10, 5, new Vector2(-10000.0f, -10000.0f));
            TowerEntity = FindEntityByName("cannonTower");
            RangePreviewTransform.Scale = new Vector3(32, 1.0f, 32);
        }
    }

    public void RemoveTowerFromHand()
    {
        if (TowerInHand != null)
        {
            TowerEntity.GetComponent<Transform>().Translation = new Vector3(-1000.0f, 0.0f, -1000.0f);
            RangePreviewTransform.Translation = new Vector3(-1000.0f, 0.0f, -1000.0f);
        }
    }
}