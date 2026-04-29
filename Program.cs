using System.Formats.Asn1;
using System.Runtime.ExceptionServices;
using Raylib_cs;

namespace HelloWorld;

internal static class Program
{
    enum GameState
    {
        Menu, 
        Playing, 
        GameOver
    }

    [System.STAThread]
    public static void Main()
    {
        Texture2D player_sprite;
        Texture2D enemy_sprite;
        
        Texture2D player_leftP;
        Texture2D player_rightP;
        Texture2D player_idle;
        Texture2D player_block;

        Texture2D enemy_leftP;
        Texture2D enemy_rightP;
        Texture2D enemy_idle;
        Texture2D enemy_block;

        Sound punchSound;
        Music music;

        Raylib.InitWindow(850, 500, "Shape Boxes! Not Beaten!");
        Raylib.SetTargetFPS(60); // FIX: stable frame timing
        Raylib.InitAudioDevice();

        player_sprite = Raylib.LoadTexture("assets/player.png");
        enemy_sprite  = Raylib.LoadTexture("assets/enemy.png");
        punchSound    = Raylib.LoadSound("assets/realpunch.ogg");
        music         = Raylib.LoadMusicStream("assets/test-music.ogg");

        player_leftP  = Raylib.LoadTexture("assets/player_leftP.png");
        player_rightP = Raylib.LoadTexture("assets/player_rightP.png");
        player_idle   = Raylib.LoadTexture("assets/player_idle.png");
        player_block  = Raylib.LoadTexture("assets/player_block.png");

        // FIX #1: was "enemyr_leftP.png" (extra r) — would crash on load
        enemy_leftP  = Raylib.LoadTexture("assets/enemy_leftP.png");
        enemy_rightP = Raylib.LoadTexture("assets/enemy_rightP.png");
        enemy_idle   = Raylib.LoadTexture("assets/enemy_idle.png");
        enemy_block  = Raylib.LoadTexture("assets/enemy_block.png");

        GameState currentState = GameState.Menu;

        // FIX: separated player and enemy positions — they were both using posX=300 and overlapping
        int playerX = 80;
        int playerY = 200;
        int enemyX  = 340;
        int enemyY  = 150;

        int playerHealth = 100;
        int enemyHealth  = 100;

        // FIX #2: impact needs a timer so it doesn't vanish the same frame it's set
        bool  showImpact  = false;
        float impactTimer = 0f;
        float impactX     = 0f;
        float impactY     = 0f;

        bool  canPunch      = true;
        float punchCooldown = 0.5f;
        float punchTimer    = 0f;

        bool isBlocking = false;

        // FIX #3: punch display timer — without this, currentPunch never resets to "none"
        string currentPunch      = "none";
        float  punchDisplayTime  = 0.25f;
        float  punchDisplayTimer = 0f;

        // FIX: enemy now has its own animation state too
        string enemyPunch             = "none";
        float  enemyPunchDisplayTimer = 0f;

        int   round       = 1;
        float roundTime   = 10f;
        float currentTime = roundTime;

        float aiTimer = 0f;
        float aiDelay = 1.2f;

        float shakeTime = 0f;
        // FIX #4: offsetX/Y moved to top scope so they persist into draw calls
        float offsetX = 0f;
        float offsetY = 0f;

        int level = 1;
        float aiDelayLevel2 = 0.7f;
        int aiDamageBonus = 0;

        int score = 0;

        Raylib.PlayMusicStream(music);

        while (!Raylib.WindowShouldClose())
        {
            Raylib.UpdateMusicStream(music);
            float dt = Raylib.GetFrameTime();

            if (currentState == GameState.Menu)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                    currentState = GameState.Playing;
            }
            else if (currentState == GameState.Playing)
            {
                isBlocking = Raylib.IsKeyDown(KeyboardKey.Space);
                // FIX #5: clamp BEFORE drawing so health bars never render negative width
                playerHealth = Math.Clamp(playerHealth, 0, 100);
                enemyHealth  = Math.Clamp(enemyHealth,  0, 100);

                if (playerHealth <= 0 || enemyHealth <= 0)
                    currentState = GameState.GameOver;

                // Punch cooldown
                punchTimer -= dt;
                if (punchTimer <= 0f) canPunch = true;

                // FIX #3 cont: tick down punch display, reset to idle when done
                if (currentPunch != "none")
                {
                    punchDisplayTimer -= dt;
                    if (punchDisplayTimer <= 0f) currentPunch = "none";
                }

                // Enemy punch display timer
                if (enemyPunch != "none")
                {
                    enemyPunchDisplayTimer -= dt;
                    if (enemyPunchDisplayTimer <= 0f) enemyPunch = "none";
                }

                // FIX #2 cont: tick impact timer down
                if (showImpact)
                {
                    impactTimer -= dt;
                    if (impactTimer <= 0f) showImpact = false;
                }

                // FIX #4 cont: shake offset updated each frame
                if (shakeTime > 0f)
                {
                    shakeTime -= dt;
                    offsetX = Raylib.GetRandomValue(-5, 5);
                    offsetY = Raylib.GetRandomValue(-5, 5);
                }
                else
                {
                    offsetX = 0f;
                    offsetY = 0f;
                }

                // Player punch input
                Rectangle enemyRect = new Rectangle(
                    enemyX + offsetX, enemyY + offsetY,
                    enemy_sprite.Width, enemy_sprite.Height);

                if (canPunch)
                {
                    if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                    {
                        currentPunch      = "left";
                        punchDisplayTimer = punchDisplayTime;
                        punchTimer        = punchCooldown;
                        canPunch          = false;

                        if (Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), enemyRect))
                        {
                            enemyHealth -= 8;
                            score       += 10;
                            Raylib.PlaySound(punchSound);
                            showImpact  = true;
                            impactTimer = 0.18f;
                            impactX     = enemyX + enemy_sprite.Width  / 2f;
                            impactY     = enemyY + enemy_sprite.Height / 3f;
                            shakeTime   = 0.2f;
                        }
                    }
                    else if (Raylib.IsMouseButtonPressed(MouseButton.Right))
                    {
                        currentPunch      = "right";
                        punchDisplayTimer = punchDisplayTime;
                        punchTimer        = punchCooldown;
                        canPunch          = false;

                        if (Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), enemyRect))
                        {
                            enemyHealth -= 12;
                            score       += 10;
                            Raylib.PlaySound(punchSound);
                            showImpact  = true;
                            impactTimer = 0.18f;
                            impactX     = enemyX + enemy_sprite.Width  / 2f;
                            impactY     = enemyY + enemy_sprite.Height / 3f;
                            shakeTime   = 0.2f;
                        }
                    }
                }

                // AI
                aiTimer += dt;
                if (aiTimer >= aiDelay)
                {
                    aiTimer = 0f;
                    int action = Raylib.GetRandomValue(0, 2);

                    if (action == 0)
                    {   
                        int dmg = isBlocking ? (2 + aiDamageBonus) : (8 + aiDamageBonus); // block absorbs most
                        playerHealth           -= dmg;
                        enemyPunch              = "left";
                        enemyPunchDisplayTimer  = punchDisplayTime;
                        shakeTime               = 0.15f;
                        showImpact              = true;
                        impactTimer             = 0.18f;
                        impactX                 = playerX + player_sprite.Width  / 2f;
                        impactY                 = playerY + player_sprite.Height / 3f;
                        Raylib.PlaySound(punchSound);
                    }
                    else if (action == 1)
                    {
                        // Miss — swing animation but no damage
                        enemyPunch             = "right";
                        enemyPunchDisplayTimer = punchDisplayTime;
                    }
                    else
                    {
                        int dmg = isBlocking ? (4 + aiDamageBonus) : (15 + aiDamageBonus);
                        playerHealth           -= dmg;
                        enemyPunch              = "right";
                        enemyPunchDisplayTimer  = punchDisplayTime;
                        shakeTime               = 0.25f;
                        showImpact              = true;
                        impactTimer             = 0.22f;
                        impactX                 = playerX + player_sprite.Width  / 2f;
                        impactY                 = playerY + player_sprite.Height / 3f;
                        Raylib.PlaySound(punchSound);
                    }
                }

                // Round timer
                currentTime -= dt;
                if (currentTime <= 0f)
                {
                    round++;
                    currentTime  = roundTime;
                    playerHealth = Math.Clamp(playerHealth + 10, 0, 100);
                    enemyHealth  = Math.Clamp(enemyHealth  + 10, 0, 100);
                
                    // Every 3 rounds, level up
                    if (round % 3 == 0)
                    {
                        level++;
                        aiDelay = Math.Max(0.5f, aiDelay - 0.2f); // gets faster, floor at 0.5s
                        aiDamageBonus += 3; // hits harder each level
                    }
                }
            }
            else if (currentState == GameState.GameOver)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.R))
                {
                    playerHealth = 100;
                    enemyHealth  = 100;
                    score        = 0;
                    round        = 1;                  // FIX: round wasn't being reset
                    currentTime  = roundTime;          // FIX: timer wasn't being reset
                    currentPunch = "none";
                    enemyPunch   = "none";
                    showImpact   = false;
                    currentState = GameState.Menu;
                }
            }

            // ================================================================
            // DRAW
            // ================================================================
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.White);

            if (currentState == GameState.Menu)
            {
                Raylib.DrawText("Shape Boxes! Not Beaten!",             195, 100, 32, Color.Black);
                Raylib.DrawText("Left Click  = Jab   (weak punch)",     255, 190, 20, Color.DarkGray);
                Raylib.DrawText("Right Click = Hook  (strong punch)",    255, 215, 20, Color.DarkGray);
                Raylib.DrawText("Click ON the enemy to land your hit!",  255, 260, 18, Color.Gray);
                Raylib.DrawText("Press ENTER to Start",                  285, 330, 22, Color.Black);
            }
            else if (currentState == GameState.Playing)
            {
                // FIX #4 cont: offsetX/Y now actually used in draw positions
                int px = (int)(playerX + offsetX);
                int py = (int)(playerY + offsetY);
                int ex = (int)(enemyX  + offsetX);
                int ey = (int)(enemyY  + offsetY);

                // FIX #6: player sprite driven by currentPunch — was always drawing player_sprite
                if      (isBlocking)              Raylib.DrawTexture(player_block, px, py, Color.White);
                else if (currentPunch == "left")  Raylib.DrawTexture(player_leftP,  px, py, Color.White);
                else if (currentPunch == "right") Raylib.DrawTexture(player_rightP, px, py, Color.White);
                else                              Raylib.DrawTexture(player_idle,    px, py, Color.White);

                // FIX #6: enemy sprite driven by enemyPunch — was always drawing enemy_sprite
                Color enemyTint = (aiTimer < 0.2f) ? Color.Red : Color.White;
                if      (enemyPunch == "left")  Raylib.DrawTexture(enemy_leftP,  ex, ey, enemyTint);
                else if (enemyPunch == "right") Raylib.DrawTexture(enemy_rightP, ex, ey, enemyTint);
                else                            Raylib.DrawTexture(enemy_idle,   ex, ey, enemyTint);

                // FIX #2 cont: impact circle now visible for its full timer duration
                if (showImpact)
                {
                    Raylib.DrawCircle((int)impactX, (int)impactY, 20, Color.Yellow);
                    Raylib.DrawCircle((int)impactX, (int)impactY, 11, Color.Orange);
                }

                // HUD
                Raylib.DrawText("Player", 50, 18, 20, Color.Black);
                Raylib.DrawRectangle(50, 42, playerHealth * 2, 20, Color.Green);
                Raylib.DrawRectangleLines(50, 42, 200, 20, Color.Black);

                Raylib.DrawText("Enemy", 600, 18, 20, Color.DarkGray);
                Raylib.DrawRectangle(600, 42, enemyHealth * 2, 20, Color.Red);
                Raylib.DrawRectangleLines(600, 42, 200, 20, Color.Black);

                Raylib.DrawText($"Score: {score}",            365, 14, 20, Color.Black);
                Raylib.DrawText($"Round: {round}",            365, 38, 18, Color.DarkGray);
                Raylib.DrawText($"Time: {(int)currentTime}s", 365, 62, 18, Color.DarkGray);
                Raylib.DrawText($"Level: {level}", 365, 86, 18, Color.Maroon);

                Raylib.DrawText("LClick = Jab | RClick = Hook | SPACE = Block | Click the enemy!", 130, 462, 16, Color.Gray);
            }
            else if (currentState == GameState.GameOver)
            {
                string result      = playerHealth <= 0 ? "DING DING!  YOU LOSE..." : "DING DING!  YOU WIN!";
                Color  resultColor = playerHealth <= 0 ? Color.Red : Color.DarkGreen;

                Raylib.DrawText(result,                    250, 150, 34, resultColor);
                Raylib.DrawText($"Final Score: {score}",  310, 210, 22, Color.Black);
                Raylib.DrawText($"Reached Round {round}", 310, 245, 18, Color.DarkGray);
                Raylib.DrawText("Press R to Play Again",  285, 300, 20, Color.Gray);
            }

            Raylib.EndDrawing();
        }

        // Cleanup — FIX: original only unloaded player_sprite, now all 8 textures freed
        Raylib.UnloadTexture(player_sprite);
        Raylib.UnloadTexture(player_idle);
        Raylib.UnloadTexture(player_leftP);
        Raylib.UnloadTexture(player_rightP);
        Raylib.UnloadTexture(player_block);
        Raylib.UnloadTexture(enemy_sprite);
        Raylib.UnloadTexture(enemy_idle);
        Raylib.UnloadTexture(enemy_leftP);
        Raylib.UnloadTexture(enemy_rightP);
        Raylib.UnloadTexture(enemy_block);
        Raylib.UnloadSound(punchSound);
        Raylib.UnloadMusicStream(music);
        Raylib.CloseAudioDevice();
        Raylib.CloseWindow();
    }
}