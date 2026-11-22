using UnityEngine;

public sealed class AstralHud
{
    static readonly Color Night = new Color32(18, 28, 47, 250);
    static readonly Color Panel = new Color32(32, 48, 71, 255);
    static readonly Color Selected = new Color32(52, 66, 84, 255);
    static readonly Color Rule = new Color32(73, 88, 105, 255);
    static readonly Color Ivory = new Color32(249, 242, 219, 255);
    static readonly Color Mist = new Color32(187, 205, 218, 255);
    static readonly Color Gold = new Color32(238, 198, 127, 255);
    static readonly Color Aqua = new Color32(135, 214, 207, 255);
    static readonly Color Coral = new Color32(242, 140, 118, 255);
    static Texture2D compass;
    AstralGame game;
    AstralMode lastMode;
    Vector2Int lens;
    int recordIndex;
    bool equipment;

    public void Browse(int dx, int dy)
    {
        if (!game) return;
        if (game.Mode == AstralMode.Map)
        {
            lens = new Vector2Int(Mathf.Clamp(lens.x + dx, 0, game.Cols - 1), Mathf.Clamp(lens.y + dy, 0, game.Rows - 1));
            equipment = false;
        }
        else if (game.Mode == AstralMode.Records)
        {
            int count = AstralGame.FoeNames.Length;
            recordIndex = (recordIndex + (dy != 0 ? -dy : dx) + count) % count;
        }
    }

    public void ToggleChartSection()
    {
        if (game && game.Mode == AstralMode.Map) equipment = !equipment;
    }

    public void Draw(AstralGame g)
    {
        game = g;
        if (g.Mode != lastMode)
        {
            if (g.Mode == AstralMode.Map) { lens = g.Hero; equipment = false; }
            if (g.Mode == AstralMode.Records) recordIndex = 0;
            lastMode = g.Mode;
        }
        switch (g.Mode)
        {
            case AstralMode.Briefing: Briefing(); break;
            case AstralMode.Explore: Explore(); break;
            case AstralMode.Relic: Relics(); break;
            case AstralMode.Map: Chart(); break;
            case AstralMode.Records: Records(); break;
            case AstralMode.Result: Result(); break;
        }
    }

    void Briefing()
    {
        Rect p = Window(855, 494, "OBSERVATORY", "観測隊の編成");
        float x = p.x + 20, y = p.y + 83;
        var art = new Rect(x, y, 320, 200);
        Illustration(art, game.Job);
        Rect bio = new Rect(x, art.yMax + 2, art.width, 133);
        Fill(bio, Panel);
        Label(AstralGame.JobNames[game.Job], bio.x + 12, bio.y + 5, bio.width - 24, 34, 23, Ivory, true);
        Label(AstralGame.JobNotes[game.Job], bio.x + 13, bio.y + 43, bio.width - 26, 76, 15, Mist);
        float sx = art.xMax + 19, sw = p.xMax - sx - 19;
        Label("航海士を選ぶ", sx, y - 2, sw, 27, 16, Gold);
        for (int i = 0; i < AstralGame.JobNames.Length; i++)
        {
            var r = new Rect(sx, y + 32 + i * 56, sw, 49);
            bool chosen = game.Job == i;
            ChoiceFrame(r, chosen, true);
            Label(AstralGame.JobNames[i], r.x + 12, r.y + 2, r.width * .57f, r.height - 4, 18, Ivory, true);
            Label(AstralGame.SkillNames[i], r.x + r.width * .53f, r.y + 2, r.width * .47f - 13, r.height - 4, 14, chosen ? Gold : Mist, align: TextAnchor.MiddleRight);
            if (Hit(r)) game.ChooseJob(i);
        }
        Label(AstralGame.SkillNotes[game.Job], sx + 2, y + 205, sw - 4, 48, 14, Mist);
        RuleAt(sx, y + 258, sw);
        Label("航路条件", sx, y + 266, 86, 26, 14, Mist);
        Label(AstralGame.ChallengeNames[game.Challenge], sx + 86, y + 260, sw - 195, 37, 17, Gold);
        if (Button(new Rect(sx + sw - 100, y + 264, 100, 30), "C　変更", game.Wins > 0, false)) game.CycleChallenge();
        Label(game.Wins > 0 ? AstralGame.ChallengeNotes[game.Challenge] : "最奥を踏破すると変則航路が開放されます。", sx + 1, y + 299, sw - 2, 35, 13, Mist);
        if (Button(new Rect(x, p.yMax - 55, 137, 37), "B　航行録", true, false)) game.OpenRecords();
        if (Button(new Rect(x + 146, p.yMax - 55, 186, 37), $"D　今日の星図 {System.DateTime.Now:M/d}", true, false)) game.StartDaily();
        Label("↑ ↓　職業選択", x + 348, p.yMax - 54, 217, 36, 14, Mist, align: TextAnchor.MiddleCenter);
        if (Button(new Rect(p.xMax - 210, p.yMax - 56, 190, 39), "Enter　出航する", true)) game.StartRun();
    }

    void Explore()
    {
        float x = 15, y = 14, w = 189;
        Plate(new Rect(x, y, w, 72));
        Star(x + 9, y + 13, 32, Gold);
        Label($"第 {game.Floor} / {game.MaxFloor} 層", x + 50, y + 5, w - 60, 27, 15, Gold);
        Label(game.FloorName, x + 12, y + 35, w - 24, 28, 17, Ivory, true);
        y += 79;
        Plate(new Rect(x, y, w, 92));
        Meter(x + 10, y + 5, w - 20, "体力", game.Hp, game.MaxHp, Coral);
        Meter(x + 10, y + 44, w - 20, "焦点", game.Focus, game.MaxFocus, Aqua);
        y += 100;
        Plate(new Rect(x, y, w, 87));
        Label($"手番　{game.Turn}", x + 10, y + 2, w - 20, 26, 17, Ivory);
        Label($"星碑 {game.Beacons} / 2　　薬 {game.Potions}", x + 11, y + 29, w - 22, 25, 14, Mist);
        Label(game.ExitReady ? "階段　開通" : "階段　封印中", x + 11, y + 56, w - 22, 23, 14, game.ExitReady ? Aqua : Gold);
        y += 96;
        var one = new Rect(x, y, w, 34);
        if (Button(one, "J　" + AstralGame.SkillNames[game.Job] + (game.SkillWait > 0 ? $"  {game.SkillWait}手" : ""), game.Focus > 0 && game.SkillWait <= 0, false)) game.UseSkill();
        if (Button(new Rect(x, y + 39, 92, 32), "F　投光", game.CanLight, false)) game.UseLight();
        if (Button(new Rect(x + 97, y + 39, 92, 32), "Q　薬", game.Potions > 0 && game.Hp < game.MaxHp, false)) game.UsePotion();
        if (Button(new Rect(x, y + 76, 92, 32), "Tab　星図", true, false)) game.OpenMap();
        if (Button(new Rect(x + 97, y + 76, 92, 32), "B　航行録", true, false)) game.OpenRecords();
        if (Button(new Rect(x, y + 113, w, 32), "Space　1手待つ", true, false)) game.WaitTurn();

        float my = y + 154;
        bool exposed = game.ThreatAt(game.Hero.x, game.Hero.y);
        string intent = EnemyWarning();
        Plate(new Rect(x, my, w, 68));
        Fill(new Rect(x, my, 3, 68), exposed ? Coral : Gold);
        Label(exposed ? "警戒　攻撃範囲内" : "次の手を読む", x + 10, my + 2, w - 20, 25, 14, exposed ? Coral : Gold);
        Label(intent ?? game.Objective, x + 10, my + 27, w - 18, 39, 13, Ivory);

        float bottom = game.H - 55;
        if (!string.IsNullOrEmpty(game.Prompt))
        {
            Rect act = new Rect(x, bottom, Mathf.Min(400, game.W * .4f), 42);
            bool usable = game.Cell(game.Hero.x,game.Hero.y) != AstralTile.Stairs || game.ExitReady;
            if (Button(act, game.Prompt, usable, false)) game.Interact();
        }
        else
        {
            Rect guide = new Rect(x, bottom, 295, 42);
            Plate(guide);
            Label("矢印 ／ WASD　移動・隣接攻撃", guide.x + 10, guide.y, guide.width - 20, guide.height, 15, Ivory);
        }
        if (!string.IsNullOrEmpty(game.LastLog))
        {
            Rect log = new Rect(game.W - 539, bottom, 523, 44);
            Plate(log);
            Label(game.LastLog, log.x + 11, log.y, log.width - 22, log.height, 14, Ivory);
        }
    }

    string EnemyWarning()
    {
        AstralFoe closest = null;
        var best = (int.MaxValue, int.MaxValue, int.MaxValue);
        foreach (var enemy in game.Enemies)
        {
            if (enemy.intent == 0 || enemy.frozen > 0 || !game.Visible(enemy.pos.x, enemy.pos.y)) continue;
            var priority = (game.TargetsHero(enemy) ? 0 : 1, enemy.delay, (enemy.pos - game.Hero).sqrMagnitude);
            if (priority.CompareTo(best) >= 0) continue;
            closest = enemy;
            best = priority;
        }
        return closest == null ? null : AstralGame.FoeNames[closest.kind] + "　" + Telegraph(closest) + $"（{closest.delay}手）";
    }

    static string Telegraph(AstralFoe enemy)
    {
        if (enemy.frozen > 0) return "封印中";
        switch (enemy.intent)
        {
            case 1: return "狙ったマスへ攻撃";
            case 2: return "直線攻撃";
            case 3: return "周囲に攻撃";
            default: return "移動・索敵";
        }
    }

    void Chart()
    {
        Rect p = Window(842, 540, "CELESTIAL CHART", "観測済みの星図");
        if (Button(new Rect(p.xMax - 166, p.y + 21, 146, 33), equipment ? "マスの観測へ" : "装備・遺物を見る", true, false))
            ToggleChartSection();
        float size = Mathf.Min(26, Mathf.Min((p.width - 303) / game.Cols, (p.height - 145) / game.Rows));
        float gx = p.x + 20, gy = p.y + 86;
        float gw = game.Cols * size, gh = game.Rows * size;
        Fill(new Rect(gx - 2, gy - 2, gw + 4, gh + 4), Rule);
        for (int cy = 0; cy < game.Rows; cy++)
        for (int cx = 0; cx < game.Cols; cx++)
        {
            Rect cell = new Rect(gx + cx * size, gy + (game.Rows - 1 - cy) * size, size, size);
            bool seen = game.Seen(cx, cy), visible = game.Visible(cx, cy);
            AstralTile tile = seen ? game.Cell(cx, cy) : AstralTile.Wall;
            Color floor = tile == AstralTile.Wall ? new Color32(44, 64, 82, 255) : new Color32(92, 116, 130, 255);
            Fill(cell, Night);
            if (!seen) continue;
            var inside = new Rect(cell.x + 1, cell.y + 1, size - 1, size - 1);
            Fill(inside, visible ? floor : Color.Lerp(Night, floor, .5f));
            if (visible && game.ThreatAt(cx, cy)) Fill(inside, new Color32(143, 83, 83, 255));
            if (tile != AstralTile.Wall && tile != AstralTile.Floor && tile != AstralTile.OpenDoor)
                Label(DescribeTile(tile).symbol, inside.x, inside.y, inside.width, inside.height, 13, visible ? Gold : Mist, align: TextAnchor.MiddleCenter);
        }
        foreach (var enemy in game.Enemies)
        {
            if (!game.Visible(enemy.pos.x,enemy.pos.y)) continue;
            float ex = gx + (enemy.pos.x + .5f) * size, ey = gy + (game.Rows - enemy.pos.y - .5f) * size;
            Fill(new Rect(ex - 6, ey - 6, 12, 12), enemy.frozen > 0 ? Aqua : Coral);
            Fill(new Rect(ex - 2, ey - 2, 4, 4), Night);
        }
        float hx = gx + game.Hero.x * size, hy = gy + (game.Rows - 1 - game.Hero.y) * size;
        Star(hx + 2, hy + 2, size - 4, Ivory);
        Rect board = new Rect(gx, gy, gw, gh);
        if (board.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            Vector2 delta = Event.current.mousePosition - board.position;
            lens = new Vector2Int(Mathf.Clamp(Mathf.FloorToInt(delta.x / size), 0, game.Cols-1),
                Mathf.Clamp(game.Rows - 1 - Mathf.FloorToInt(delta.y / size), 0, game.Rows-1));
            equipment = false;
            Event.current.Use();
        }
        lens = new Vector2Int(Mathf.Clamp(lens.x,0,game.Cols-1),Mathf.Clamp(lens.y,0,game.Rows-1));
        Border(new Rect(gx + lens.x*size, gy + (game.Rows-1-lens.y)*size, size, size), Gold);

        float sx = board.xMax + 19, sw = p.xMax - sx - 19;
        if (equipment)
        {
            Label(AstralGame.JobNames[game.Job], sx, gy, sw, 34, 20, Ivory, true);
            Label($"攻撃力 {game.AttackPower}　 薬 {game.Potions} 個", sx + 1, gy + 39, sw - 2, 29, 15, Mist);
            Label($"体力 {game.Hp} / {game.MaxHp}　 焦点 {game.Focus} / {game.MaxFocus}", sx + 1, gy + 70, sw - 2, 29, 15, Mist);
            RuleAt(sx, gy + 108, sw);
            Label(AstralGame.SkillNames[game.Job], sx + 1, gy + 118, sw - 2, 27, 17, Gold);
            Label(AstralGame.SkillNotes[game.Job], sx + 1, gy + 150, sw - 2, 72, 15, Ivory);
            Label(game.SkillWait > 0 ? $"職技の再使用まで {game.SkillWait} 手" : "職技　再使用可能", sx + 1, gy + 222, sw - 2, 25, 13, Aqua);
            RuleAt(sx, gy + 255, sw);
            Label($"携行中の遺物　{game.RelicCount} 個", sx + 1, gy + 266, sw - 2, 26, 15, Gold);
            Label(string.IsNullOrEmpty(game.Loadout) ? "遺物なし" : game.Loadout, sx + 1, gy + 296, sw - 2, 85, 14, Ivory);
        }
        else
        {
            Label($"第 {game.Floor} 層　{game.FloorName}", sx, gy, sw, 34, 19, Ivory, true);
            Label($"手番 {game.Turn}　 星碑 {game.Beacons}/2", sx + 1, gy + 38, sw - 2, 26, 14, Mist);
            Label(game.Objective, sx + 1, gy + 67, sw - 2, 54, 15, Ivory);
            RuleAt(sx, gy + 128, sw);
            Label($"観測座標　{lens.x + 1}, {lens.y + 1}", sx + 1, gy + 139, sw - 2, 26, 15, Gold);
            bool known = game.Seen(lens.x,lens.y);
            string description = known ? DescribeTile(game.Cell(lens.x,lens.y)).name : "未観測の区画";
            foreach (var enemy in game.Enemies)
            {
                if (enemy.pos != lens || !game.Visible(lens.x,lens.y)) continue;
                description = AstralGame.FoeNames[enemy.kind] + $"　HP {enemy.hp}/{enemy.maxHp}\n" + Telegraph(enemy) + (enemy.intent != 0 && enemy.frozen <= 0 ? $"・あと {enemy.delay} 手" : "");
                break;
            }
            Label(description, sx + 1, gy + 168, sw - 2, 62, 16, Ivory);
            RuleAt(sx, gy + 237, sw);
            Label("星印　自分　／　赤い印　魔物\n赤い床　予告された攻撃範囲\n金枠　観測中のマス", sx + 1, gy + 248, sw - 2, 80, 14, Mist);
            Label($"星図番号　{game.Seed}", sx + 1, gy + 335, sw - 2, 27, 14, Gold);
            Label(game.RouteLabel, sx + 1, gy + 364, sw - 2, 25, 13, Mist);
        }
        Label("矢印またはクリックでマスを観測", gx, p.yMax - 52, gw, 34, 14, Mist);
        if (Button(new Rect(p.xMax - 174, p.yMax - 54, 154, 38), "探索へ戻る", true, false)) game.Back();
    }

    static (string name, string symbol) DescribeTile(AstralTile tile) => tile switch
    {
        AstralTile.Wall        => ("観測所の壁", ""),
        AstralTile.Door        => ("閉じた扉", "扉"),
        AstralTile.OpenDoor    => ("開いた扉", ""),
        AstralTile.Stairs      => ("次の階への階段", "階"),
        AstralTile.Chest       => ("宝箱", "箱"),
        AstralTile.Shrine      => ("星の祭壇", "祠"),
        AstralTile.Trap        => ("罠", "!"),
        AstralTile.Potion      => ("回復薬", "薬"),
        AstralTile.Crystal     => ("焦点の結晶", "晶"),
        AstralTile.Beacon      => ("未点灯の星碑", "碑"),
        AstralTile.SpentBeacon => ("点灯済みの星碑", "灯"),
        _                      => ("通路", "")
    };

    void Relics()
    {
        Rect p = Window(806, 333, "STELLAR RELICS", "遺物をひとつ選ぶ");
        Label("この航海に持っていく遺物を選びましょう。", p.x + 21, p.y + 78, p.width - 42, 31, 15, Mist);
        float w = (p.width - 60) / 3;
        for (int i = 0; i < 3; i++)
        {
            var r = new Rect(p.x + 20 + (w + 10)*i, p.y + 118, w, 153);
            ChoiceFrame(r, game.Selection == i, true);
            Star(r.center.x - 13, r.y + 11, 26, game.Selection == i ? Gold : Aqua);
            Label(game.RelicName(i), r.x + 9, r.y + 42, w - 18, 35, 19, Ivory, true, TextAnchor.MiddleCenter);
            RuleAt(r.x + 15, r.y + 82, w - 30);
            Label(game.RelicText(i), r.x + 11, r.y + 85, w - 22, 60, 14, Mist, align: TextAnchor.MiddleCenter);
            if (Hit(r)) game.SelectRelic(i);
        }
        Label("矢印で選択　 Enter で決定", p.x + 21, p.yMax - 48, p.width - 42, 32, 14, Mist);
    }

    void Records()
    {
        Rect p = Window(788, 483, "OBSERVATION LOG", "航行録");
        Metrics(p, 83, new[] { "これまでの航海", "到達した最深層", "踏破した航海" },
            new[] { game.Runs.ToString(), $"{game.BestFloor} / {game.MaxFloor}", game.Wins.ToString() });
        Label($"魔物の観測　{game.SightCount} / {AstralGame.FoeNames.Length}", p.x + 21, p.y + 165, p.width - 42, 31, 19, Ivory, true);
        float sx = p.x + 20, sy = p.y + 207, width = 227;
        recordIndex = Mathf.Clamp(recordIndex, 0, AstralGame.FoeNames.Length-1);
        for (int i = 0; i < AstralGame.FoeNames.Length; i++)
        {
            var r = new Rect(sx, sy + i * 35, width, 30);
            bool known = game.HasSeenFoe(i);
            ChoiceFrame(r, i == recordIndex, true);
            Label(known ? AstralGame.FoeNames[i] : "未観測", r.x + 10, r.y + 1, width - 20, 29, 14, known ? Ivory : Mist);
            if (Hit(r)) recordIndex = i;
        }
        float bx = sx + width + 13, bw = p.xMax - bx - 20;
        Fill(new Rect(bx,sy,bw,171), Panel);
        Rect portrait = new Rect(bx + 9, sy + 11, 146, 146);
        Fill(portrait, Night);
        bool revealed = game.HasSeenFoe(recordIndex);
        if (revealed) Specimen(portrait, recordIndex);
        else Star(portrait.center.x - 26, portrait.center.y - 26, 52, Rule);
        Label(revealed ? AstralGame.FoeNames[recordIndex] : "未観測の魔物", bx + 168, sy + 8, bw - 180, 34, 20, Ivory, true);
        Label(revealed ? AstralGame.FoeNotes[recordIndex] : "探索中に出会うと、ここに観測記録が残ります。", bx + 169, sy + 46, bw - 184, 112, 15, Mist);
        string best = game.BestChallenge == 0 ? "未踏破" : AstralGame.ChallengeNames[game.BestChallenge];
        Label($"変則航路の最高記録　{best}", p.x + 22, p.yMax - 68, p.width - 230, 39, 14, Gold);
        if (Button(new Rect(p.xMax - 167, p.yMax - 57, 147, 38), "戻る", true, false)) game.Back();
    }

    void Result()
    {
        Rect p = Window(790, 444, game.Won ? "VOYAGE COMPLETE" : "END OF VOYAGE", game.Won ? "最奥の観測を終えて" : "航海の記録");
        float bx = p.x + 20, by = p.y + 83;
        Illustration(new Rect(bx, by, 224, 143), game.Won ? 3 : game.Job);
        Label(game.ResultText, bx + 242, by, p.width - 282, 95, 17, Ivory);
        Label($"{AstralGame.JobNames[game.Job]}　・　{AstralGame.ChallengeNames[game.Challenge]}", bx + 242, by + 99, p.width - 282, 42, 15, Gold);
        Metrics(p, 241, new[] { "到達した階", "使った手番", "倒した魔物" },
            new[] { $"{game.Floor} / {game.MaxFloor}", game.Turn.ToString(), game.Kills.ToString() });
        Label($"{game.RouteLabel}　　星図番号 {game.Seed}　　遺物 {game.RelicCount} 個", bx, p.y + 317, p.width - 40, 33, 15, Mist);
        float w = (p.width - 58) / 3;
        string[] names = { "新しい航海へ", "同じ星図で再航", "航行録を開く" };
        for (int i = 0; i < names.Length; i++)
        {
            var r = new Rect(bx + i*(w + 9), p.yMax - 62, w, 43);
            if (!Button(r, names[i], true, game.Selection == i)) continue;
            if (i == 0) game.NewVoyage(); else if (i == 1) game.ReplaySeed(); else game.OpenRecords();
        }
    }

    void Metrics(Rect p, float offsetY, string[] labels, string[] values)
    {
        float w = (p.width - 56) / 3;
        for (int i=0; i<3; i++)
        {
            float x = p.x + 20 + (w+8)*i, y = p.y + offsetY;
            Fill(new Rect(x,y,w,64),Panel);
            Label(labels[i],x+10,y+4,w-20,23,13,Mist);
            Label(values[i],x+10,y+27,w-20,31,21,Gold,true);
        }
    }

    void Meter(float x, float y, float w, string name, int value, int max, Color color)
    {
        Label(name,x,y,w*.45f,23,14,Ivory);
        Label($"{value} / {max}",x+w*.42f,y,w*.58f,23,14,Mist,align:TextAnchor.MiddleRight);
        Fill(new Rect(x,y+26,w,5),Rule);
        float fill=w*Mathf.Clamp01(value/(float)Mathf.Max(1,max));if(fill>0)Fill(new Rect(x,y+26,fill,5),color);
    }

    Rect Window(float w,float h,string subtitle,string title)
    {
        Fill(new Rect(0,0,game.W,game.H),new Color(.012f,.023f,.055f,.68f));
        Rect p=new Rect((game.W-w)/2,(game.H-h)/2,w,h);Plate(p);
        Star(p.x+18,p.y+16,36,Gold);
        Label(subtitle,p.x+68,p.y+9,w-90,20,12,Gold);
        Label(title,p.x+66,p.y+29,w-88,35,25,Ivory,true);
        RuleAt(p.x+17,p.y+70,w-34);
        return p;
    }

    void Illustration(Rect frame,int index)
    {
        Fill(frame,Panel);
        var texture=game.skin.illustrations;if(!texture)return;
        float ratio=texture.width/(float)texture.height;
        float w=Mathf.Max(frame.width,frame.height*ratio),h=w/ratio;
        GUI.BeginGroup(frame);
        game.Art(new Rect((frame.width-w)/2,(frame.height-h)/2,w,h),index);
        GUI.EndGroup();
    }

    void Specimen(Rect frame, int index)
    {
        var sheet = game.skin.specimenPlate;
        if (!sheet) return;
        // The engraved sheet uses a shorter upper row and a taller row for the standing figures.
        const float rowDivider = .412f;
        float top = index < 3 ? 0 : rowDivider;
        float sourceHeight = index < 3 ? rowDivider : 1 - rowDivider;
        float scale = Mathf.Min(frame.width / (sheet.width / 3f), frame.height / (sheet.height * sourceHeight));
        float fullW = sheet.width * scale, fullH = sheet.height * scale;
        float w = fullW / 3f, h = fullH * sourceHeight;
        GUI.BeginGroup(new Rect(frame.center.x - w / 2, frame.center.y - h / 2, w, h));
        game.Plate(new Rect(-(index % 3) * w, -top * fullH, fullW, fullH), 0, 1, 1);
        GUI.EndGroup();
    }

    bool Button(Rect r,string words,bool enabled,bool primary=true)
    {
        bool hover=enabled && r.Contains(Event.current.mousePosition);
        Fill(r,enabled?(primary?Gold:hover?Selected:Panel):Night);
        Border(r,enabled?(primary?Gold:hover?Gold:Rule):Rule);
        Label(words,r.x+4,r.y,r.width-8,r.height,15,enabled&&primary?Night:enabled?Ivory:Mist,align:TextAnchor.MiddleCenter);
        return enabled&&Hit(r);
    }
    void ChoiceFrame(Rect r,bool active,bool enabled)
    {
        bool hover=enabled&&r.Contains(Event.current.mousePosition);
        Fill(r,active||hover?Selected:Panel);
        if(active||hover)Fill(new Rect(r.x,r.y,3,r.height),Gold);
    }
    static bool Hit(Rect r)=>GUI.Button(r,GUIContent.none,GUIStyle.none);
    void Plate(Rect r)
    {
        Fill(new Rect(r.x+2,r.y+3,r.width,r.height),new Color(0,0,0,.3f));
        Fill(r,Night);RuleAt(r.x,r.y,r.width);
    }
    void RuleAt(float x,float y,float w)=>Fill(new Rect(x,y,w,1),Rule);
    void Border(Rect r,Color color)
    {
        Fill(new Rect(r.x,r.y,r.width,1),color);Fill(new Rect(r.x,r.yMax-1,r.width,1),color);
        Fill(new Rect(r.x,r.y,1,r.height),color);Fill(new Rect(r.xMax-1,r.y,1,r.height),color);
    }
    void Fill(Rect r,Color c)=>game.Box(r,c);
    void Label(string t,float x,float y,float w,float h,int size,Color color,bool serif=false,TextAnchor align=TextAnchor.MiddleLeft)
    {
        if(Screen.width<800&&size>=12&&size<=15)size++;
        game.Label(new Rect(x,y,w,h),t,size,color,serif,align);
    }

    // An astrolabe mark rendered once. The transparent border keeps the cardinal points visible at small sizes.
    static void Star(float x,float y,float size,Color tint)
    {
        if(Event.current.type!=EventType.Repaint)return;
        if(!compass)
        {
            const int n=72;var pixels=new Color32[n*n];
            for(int iy=0;iy<n;iy++)
            for(int ix=0;ix<n;ix++)
            {
                float ax=Mathf.Abs((ix+.5f)/n-.5f),ay=Mathf.Abs((iy+.5f)/n-.5f);
                float radius=Mathf.Sqrt(ax*ax+ay*ay);
                float ring=Mathf.Clamp01((.023f-Mathf.Abs(radius-.32f))*n);
                float ray=Mathf.Min(ax+.27f*ay,ay+.27f*ax);
                float point=Mathf.Clamp01((.106f-ray)*n)*Mathf.Clamp01((.475f-Mathf.Max(ax,ay))*n);
                pixels[iy*n+ix]=new Color32(255,255,255,(byte)(255*Mathf.Max(ring,point)));
            }
            compass=new Texture2D(n,n,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            compass.SetPixels32(pixels);compass.Apply(false,true);
        }
        GUI.DrawTexture(new Rect(x,y,size,size),compass,ScaleMode.ScaleToFit,true,1,tint,0,0);
    }
}
