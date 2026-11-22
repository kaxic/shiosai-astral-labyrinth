using System;
using System.Collections.Generic;
using UnityEngine;

public enum AstralTile { Wall, Floor, Door, OpenDoor, Stairs, Chest, Shrine, Trap, Potion, Crystal, Beacon, SpentBeacon }
public enum AstralMode { Briefing, Explore, Relic, Map, Records, Result }

[Serializable]
public sealed class AstralFoe
{
    public int id, kind, hp, maxHp, intent, frozen, delay, pattern, alerted;
    public Vector2Int pos, aim;
}

[Serializable]
public sealed class AstralRecord
{
    public int version = 1, bestFloor, wins, runs, totalKills, foeBits, challengeBits, bestChallenge;
    public AstralMode mode = AstralMode.Briefing, beforeRecords;
    public int seed = 74281, dailyDate, job, floor = 1, turn, enemyPhases, floorTurn, challenge, kills;
    public int hp, focus, potions, skillWait, lit, guard, beacons, exploredSteps, freeLight, nextId = 1;
    public bool won;
    public Vector2Int hero, facing = Vector2Int.up, stairs;
    public AstralTile[] tiles;
    public bool[] seen, visible, foundTraps, visited;
    public List<AstralFoe> enemies = new List<AstralFoe>();
    public List<int> relics = new List<int>();
    public int[] offers = new int[3];
    public string log = "", lastAction = "";
}

public static class AstralChart
{
    public const int Width = 19, Height = 15, Size = Width * Height, Summit = 10;
    public static readonly Vector2Int[] Steps = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
    public static bool Inside(Vector2Int p) => p.x >= 0 && p.x < Width && p.y >= 0 && p.y < Height;
    public static int Id(Vector2Int p) => p.y * Width + p.x;
    public static Vector2Int Point(int i) => new Vector2Int(i % Width, i / Width);
    public static int Distance(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    public static bool Open(AstralRecord s, Vector2Int p, bool doors = false) => Inside(p) &&
        s.tiles[Id(p)] != AstralTile.Wall && (doors || s.tiles[Id(p)] != AstralTile.Door);
    public static int FloorSeed(int seed, int floor, int challenge) => unchecked(seed * 397 ^ floor * 1103515245 ^ challenge * 7919);

    public static int[] Distances(AstralRecord s, Vector2Int start, bool throughDoors)
    {
        var distance = new int[Size];
        Array.Fill(distance, -1);
        var queue = new int[Size]; int first = 0, last = 0;
        int at = Id(start); queue[last++] = at; distance[at] = 0;
        while (first < last)
        {
            int cell = queue[first++]; var p = Point(cell);
            foreach (var step in Steps)
            {
                var next = p + step;
                if (!Open(s, next, throughDoors)) continue;
                int i = Id(next); if (distance[i] >= 0) continue;
                distance[i] = distance[cell] + 1; queue[last++] = i;
            }
        }
        return distance;
    }

    // Bresenham sight, showing the first wall/closed door itself but never through it.
    public static bool Sight(AstralRecord s, Vector2Int a, Vector2Int b)
    {
        int dx = Math.Abs(b.x - a.x), dy = -Math.Abs(b.y - a.y);
        int sx = a.x < b.x ? 1 : -1, sy = a.y < b.y ? 1 : -1, error = dx + dy;
        while (a != b)
        {
            int twice = error * 2;
            if (twice >= dy) { error += dy; a.x += sx; }
            if (twice <= dx) { error += dx; a.y += sy; }
            if (a != b && !Open(s, a)) return false;
        }
        return true;
    }

    struct Link
    {
        public int a,b;
        public Link(int a,int b) { this.a=a; this.b=b; }
    }
    static void Shuffle<T>(IList<T> list, System.Random random)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1); T old = list[i]; list[i] = list[j]; list[j] = old;
        }
    }

    public static void Generate(AstralRecord s)
    {
        var random = new System.Random(FloorSeed(s.seed, s.floor, s.challenge));
        s.tiles = new AstralTile[Size]; s.seen = new bool[Size]; s.visible = new bool[Size];
        s.foundTraps = new bool[Size]; s.visited = new bool[Size];
        s.enemies.Clear(); s.floorTurn = s.beacons = s.lit = s.guard = s.skillWait = 0;
        var centers = new Vector2Int[6];
        void Carve(Vector2Int p) { if (p.x > 0 && p.y > 0 && p.x < Width - 1 && p.y < Height - 1) s.tiles[Id(p)] = AstralTile.Floor; }
        for (int n = 0; n < centers.Length; n++)
        {
            var center = new Vector2Int(3 + n % 3 * 6 + random.Next(-1, 2), 3 + n / 3 * 7 + random.Next(-1, 2));
            centers[n] = center;
            int rx = random.Next(2) + 1, ry = random.Next(2) + 1;
            int shape = random.Next(4);
            for (int y = -ry; y <= ry; y++)
                for (int x = -rx; x <= rx; x++)
                {
                    // Cross-shaped galleries, bevelled corners, rectangular halls and offset alcoves.
                    if (shape == 0 && x != 0 && y != 0 && (Math.Abs(x) == rx || Math.Abs(y) == ry)) continue;
                    if (shape == 1 && Math.Abs(x) == rx && Math.Abs(y) == ry) continue;
                    Carve(center + new Vector2Int(x, y));
                }
            if (shape == 3) Carve(center + new Vector2Int(rx + 1, 0));
        }
        var links = new List<Link> { new Link(0,1),new Link(1,2),new Link(3,4),new Link(4,5),
            new Link(0,3),new Link(1,4),new Link(2,5) };
        Shuffle(links, random);
        var groups = new[] {0,1,2,3,4,5};
        int Group(int i) { while (groups[i] != i) i = groups[i]; return i; }
        var doors = new List<Vector2Int>();
        foreach (var link in links)
        {
            int a = Group(link.a), b = Group(link.b);
            if (a == b && random.Next(100) > 42) continue;
            groups[a] = b;
            Vector2Int p = centers[link.a], end = centers[link.b]; bool horizontal = random.Next(2) == 0;
            var corridor = new List<Vector2Int>();
            while (p != end)
            {
                if ((horizontal && p.x != end.x) || p.y == end.y) p.x += Math.Sign(end.x - p.x);
                else p.y += Math.Sign(end.y - p.y);
                Carve(p); corridor.Add(p);
            }
            if (corridor.Count >= 4) doors.Add(corridor[corridor.Count / 2]);
        }
        // The outer rings are navigable landmarks connecting the six procedural galleries.
        if (s.floor >= 8)
        {
            foreach (var c in centers)
            {
                int y = c.y;
                while (y != Height/2) { Carve(new Vector2Int(c.x,y)); y += Math.Sign(Height/2-y); }
            }
            for(int x=2;x<Width-2;x++) Carve(new Vector2Int(x,Height/2));
            if(s.floor==8 || s.floor==10)
            {
                for(int x=3;x<=Width-4;x++){Carve(new Vector2Int(x,3));Carve(new Vector2Int(x,Height-4));}
                for(int y=3;y<=Height-4;y++){Carve(new Vector2Int(3,y));Carve(new Vector2Int(Width-4,y));}
            }
            if(s.floor>=9)
            {
                for(int y=2;y<Height-2;y++) Carve(new Vector2Int(Width/2,y));
                for(int dx=-2;dx<=2;dx++)for(int dy=-2;dy<=2;dy++)
                    if(Math.Abs(dx)+Math.Abs(dy)<=3)Carve(new Vector2Int(Width/2+dx,Height/2+dy));
            }
        }
        s.hero = centers[random.Next(centers.Length)]; s.facing = Vector2Int.up;
        var dist = Distances(s, s.hero, true);
        s.stairs = s.hero;
        foreach (var c in centers) if (dist[Id(c)] > dist[Id(s.stairs)]) s.stairs = c;
        s.tiles[Id(s.stairs)] = AstralTile.Stairs;
        foreach (var d in doors)
        {
            if (d == s.hero || d == s.stairs || Array.IndexOf(centers, d) >= 0) continue;
            if (random.Next(100) < (s.challenge == 3 ? 90 : 64)) s.tiles[Id(d)] = AstralTile.Door;
        }
        var branches = new List<Vector2Int>(centers);
        branches.Remove(s.hero); branches.Remove(s.stairs); Shuffle(branches, random);
        for (int i = 0; i < 2; i++) s.tiles[Id(branches[i])] = AstralTile.Beacon;
        var spare = new List<int>();
        for (int i = 0; i < Size; i++) if (dist[i] >= 2 && s.tiles[i] == AstralTile.Floor) spare.Add(i);
        Shuffle(spare, random); int cursor = 0;
        void Place(AstralTile tile, int count)
        {
            for (int i = 0; i < count && cursor < spare.Count; i++) s.tiles[spare[cursor++]] = tile;
        }
        Place(AstralTile.Shrine, 1); Place(AstralTile.Chest, 2);
        Place(AstralTile.Potion, s.floor <= 2 ? 2 : 1); Place(AstralTile.Crystal, 2);
        Place(AstralTile.Trap, 2 + s.floor / 3 + (s.challenge == 3 ? 3 : 0));
        void Spawn(int kind, Vector2Int p)
        {
            int hp = new[] {6, 5, 7, 6, 19, 5, 8, 6, 9, 8}[kind] + Mathf.Min(7,s.floor) * (kind == 4 ? 2 : 1);
            s.enemies.Add(new AstralFoe { id = s.nextId++, kind = kind, pos = p, aim = p, hp = hp, maxHp = hp });
        }
        if (s.floor == 4 || s.floor == 7 || s.floor == Summit) Spawn(4, s.stairs);
        int target = 2 + Mathf.Min(6,s.floor + 1) / 2 + (s.challenge == 2 ? 1 : 0);
        for (int i = 0; i < target && cursor < spare.Count; cursor++)
        {
            int cell = spare[cursor];
            if (dist[cell] < 5 || s.tiles[cell] != AstralTile.Floor) continue;
            int kind = s.floor <= 2 ? random.Next(3) : random.Next(4);
            if (s.floor >= 3 && i % 2 == 0) kind = 5 + ((s.floor - 3 + i/2) % 5);
            if (s.challenge == 2 && i == 0) kind = 1;
            Spawn(kind, Point(cell)); i++;
        }
        s.visited[Id(s.hero)] = true;
    }
}
