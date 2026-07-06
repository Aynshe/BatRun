// EN: Partial class for ArcadeApiService - Connect page HTML generation (login only)
// FR: Classe partielle pour ArcadeApiService - Generation HTML de la page Connect (login uniquement)
// EN: This file contains GetConnectPageHtml() and GetStaticNodeHtml() - ZERO game/streaming code
// FR: Ce fichier contient GetConnectPageHtml() et GetStaticNodeHtml() - AUCUN code jeu/streaming

using System;
using System.Linq;

using BatRun;
using BatRun.Core;

namespace BatRun.Core
{
    public partial class ArcadeApiService
    {
        private string GetStaticNodeHtml(bool isExternal)
        {
            try
            {
                var machinesList = _manager.GetNetworkMachines()
                    .Select(m => new { 
                        name = m.Name,
                        ip = m.IP, // [BATRUN-IRON]: Always send real IP for direct links
                        port = m.Port,
                        isLocal = m.IsLocal,
                        isOnline = m.IsOnline,
                        requiresLogin = m.RequiresLogin,
                        isMoonlightEnabled = m.IsMoonlightEnabled
                    })
                    .ToList();

                // EN: Ensure local machine is always present / FR: S'assurer que la machine locale est toujours présente
                if (!machinesList.Any(m => m.isLocal))
                {
                    machinesList.Add(new { 
                        name = Environment.MachineName, 
                        ip = "127.0.0.1", 
                        port = _port, 
                        isLocal = true, 
                        isOnline = true,
                        requiresLogin = _manager.PublicAccessRequiresLogin,
                        isMoonlightEnabled = _manager.MoonlightStreamEnabled
                    });
                }

                var final = machinesList.OrderByDescending(m => m.isLocal).ThenBy(m => m.name).ToList();
                var sb = new System.Text.StringBuilder();
                foreach (var m in final)
                {
                    string activeClass = m.isLocal ? "active" : "";
                    string onlineClass = m.isOnline ? "online" : "";
                    string ipLabel = m.isLocal ? "LOCAL" : ((!isExternal && !_manager.HubMode) ? m.ip : "REMOTE");
                    bool reqLog = m.isLocal ? _manager.PublicAccessRequiresLogin : m.requiresLogin; // [BATRUN-IRON]: Direct live state for local
                    bool isMl = m.isLocal ? _manager.MoonlightStreamEnabled : m.isMoonlightEnabled;
                    
                    // EN: Escape single quotes for JS / FR: Echapper les guillemets simples pour JS
                    string safeName = m.name.Replace("'", "\\'");
                    string safeIp = m.ip.Replace("'", "\\'");
                    
                    sb.Append($@"
                        <div class=""node-tile {activeClass}"" data-name=""{m.name}"" data-requires-login=""{reqLog.ToString().ToLower()}"" onclick=""selectNode({{name:'{safeName}', ip:'{safeIp}', port:{m.port}, isLocal:{m.isLocal.ToString().ToLower()}, requiresLogin:{reqLog.ToString().ToLower()}, isMoonlightEnabled:{isMl.ToString().ToLower()}}}, this)"">
                            <div class=""node-status {onlineClass}""></div>
                            <div class=""node-name"" style=""font-weight:bold; font-size:0.9rem; overflow:hidden; text-overflow:ellipsis; white-space:nowrap;"">{m.name}</div>
                            <div class=""node-ip"" style=""font-size:0.7rem; opacity:0.6;"">{ipLabel}</div>
                        </div>");
                }
                return sb.ToString();
            }
            catch { return ""; }
        }


        private string GetConnectPageHtml(bool canAccessAdmin, bool isExternal)
        {
            string html = @"<!DOCTYPE html>
<html lang=""fr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>BatRun Access</title>
    <link rel=""preconnect"" href=""https://fonts.googleapis.com"">
    <link rel=""preconnect"" href=""https://fonts.gstatic.com"" crossorigin>
    <link href=""https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;600&display=swap"" rel=""stylesheet"">
    <style>
        :root {
            --primary: #00f2ff;
            --secondary: #7000ff;
            --accent: #ff00c8;
            --bg: #050510;
            --card-bg: rgba(255, 255, 255, 0.03);
            --card-border: rgba(255, 255, 255, 0.1);
            --text: #ffffff;
            --success: #00ff88;
        }

        * { box-sizing: border-box; }
        body { 
            margin: 0; padding: 0; background: var(--bg); color: var(--text); 
            font-family: 'Outfit', sans-serif; height: 100vh; overflow: hidden;
            display: flex; flex-direction: column; align-items: center; justify-content: center;
        }

        /* Animated Background */
        .bg-glow {
            position: fixed; top: 0; left: 0; width: 100%; height: 100%; z-index: -1;
            background: radial-gradient(circle at 20% 30%, rgba(112, 0, 255, 0.15), transparent 40%),
                        radial-gradient(circle at 80% 70%, rgba(0, 242, 255, 0.15), transparent 40%);
            filter: blur(80px); animation: pulseBg 15s infinite alternate;
        }
        @keyframes pulseBg {
            0% { opacity: 0.5; transform: scale(1); }
            100% { opacity: 0.8; transform: scale(1.1); }
        }

        .container { 
            background: var(--card-bg); backdrop-filter: blur(20px); -webkit-backdrop-filter: blur(20px);
            padding: 40px; border-radius: 24px; border: 1px solid var(--card-border);
            text-align: center; max-width: 450px; width: 90%; 
            box-shadow: 0 20px 50px rgba(0,0,0,0.5); transform: translateY(0);
            transition: all 0.5s cubic-bezier(0.175, 0.885, 0.32, 1.275);
        }
        
        #gameContainer { max-width: 1200px; width: 100%; height: 100vh; display: flex; flex-direction: column; margin: 0 auto; overflow: hidden; padding: 20px; }

        /* Loader Animation */
        .loader-spinner {
            width: 40px; height: 40px; border: 4px solid rgba(255, 255, 255, 0.2);
            border-top: 4px solid var(--primary); border-radius: 50%;
            animation: spin 1s linear infinite; margin-top: 15px;
        }
        @keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }

        /* Custom System Select Grid */
        #systemSelectModal {
            position: absolute; top: 0; left: 0; width: 100%; height: 100%;
            background: rgba(15, 23, 42, 0.95); backdrop-filter: blur(10px);
            z-index: 10000; display: none; flex-direction: column; align-items: center; padding-top: 50px;
        }
        .system-grid {
            display: grid; grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
            gap: 15px; width: 90%; max-height: 70vh; overflow-y: auto; padding: 20px;
        }
        .system-option {
            background: rgba(255,255,255,0.05); border: 2px solid transparent; border-radius: 12px;
            padding: 15px; text-align: center; cursor: pointer; transition: all 0.2s;
            font-weight: bold; color: white; display: flex; align-items: center; justify-content: center;
        }
        .system-option:hover, .system-option.focused {
            border-color: var(--primary); background: rgba(0, 210, 255, 0.1); transform: scale(1.05);
        }
        
        /* Virtual Keyboard */
        .virtual-keyboard {
            width: 100%; box-sizing: border-box;
            margin: 12px 0 5px 0; background: rgba(10, 10, 15, 0.98);
            border: 2px solid var(--primary); border-radius: 12px; padding: 10px;
            display: none; flex-direction: column; gap: 6px; z-index: 10000; box-shadow: 0 10px 40px rgba(0,0,0,0.9);
            pointer-events: all;
        }
        .vk-row { display: flex; justify-content: center; gap: 5px; width: 100%; }
        .vk-key {
            background: rgba(255,255,255,0.1); border: 2px solid transparent; border-radius: 6px;
            color: white; padding: 11px 0; font-weight: bold; cursor: pointer; text-align: center;
            flex: 1; min-width: 20px; font-size: 0.88em;
        }
        .vk-key.focused { background: var(--primary); color: black; border-color: white; transform: scale(1.1); z-index: 10; }

        /* Fullscreen: hide UI elements in game mode */
        #streamView:fullscreen #btnStopGame,
        #streamView:-webkit-full-screen #btnStopGame {
            display: none !important;
        }
        #streamView:fullscreen .stream-overlay,
        #streamView:-webkit-full-screen .stream-overlay {
            display: none !important;
        }

        h1 { margin-top: 0; font-weight: 600; letter-spacing: -0.5px; background: linear-gradient(90deg, var(--primary), var(--secondary)); -webkit-background-clip: text; -webkit-text-fill-color: transparent; }
        
        .input-group { position: relative; margin-bottom: 20px; }
        input { 
            width: 100%; padding: 14px 20px; border: 1px solid var(--card-border); border-radius: 12px; 
            background: rgba(0,0,0,0.3); color: white; transition: all 0.3s ease; font-size: 16px;
        }
        input:focus { outline: none; border-color: var(--primary); box-shadow: 0 0 15px rgba(0, 242, 255, 0.3); }

        button { 
            width: 100%; padding: 14px; background: linear-gradient(135deg, var(--primary), var(--secondary));
            color: #000; border: none; border-radius: 12px; font-weight: 600; cursor: pointer; 
            transition: all 0.3s ease; box-shadow: 0 5px 15px rgba(0,0,0,0.3);
        }
        button:hover { transform: translateY(-2px); box-shadow: 0 8px 20px rgba(0, 242, 255, 0.4); }
        button:active { transform: translateY(0); }
        h1, h2, h3 { margin-bottom: 0.5em; }

        /* Focus state for gamepad navigation */
        .game-item.focused, #customSystemSelect.focused, #gameSearchInput.focused, #btnOpenRbModal.focused, #lobbyJoinBannerBtn.focused {
            outline: 3px solid var(--primary) !important;
            box-shadow: 0 0 20px rgba(0, 210, 255, 0.6) !important;
            transform: scale(1.03) !important;
            z-index: 10;
        }

        /* Responsive Banner */
        @media (max-width: 650px) {
            #lobbyJoinBanner {
                flex-direction: column;
                text-align: center;
                gap: 12px !important;
                padding: 15px !important;
            }
            #lobbyJoinBanner > div {
                justify-content: center;
                width: 100%;
            }
            #lobbyJoinBannerBtn {
                width: auto !important;
                max-width: 200px;
                min-width: 120px !important;
                padding: 10px 20px !important;
                margin: 0 auto;
            }
            #lobbyJoinBannerText {
                font-size: 1rem !important;
                text-align: center;
                white-space: normal !important;
            }
            .lobby-choice {
                width: 100%;
                box-sizing: border-box;
                padding: 12px 20px;
            }
        }

        /* [BATRUN-v3] Emergency RetroBat Launch Button */
        .emergency-rb-btn {
            display: inline-block;
            padding: 15px 30px;
            background: linear-gradient(135deg, #ff416c 0%, #ff4b2b 100%);
            color: white;
            text-decoration: none;
            border-radius: 12px;
            font-weight: bold;
            margin-top: 20px;
            box-shadow: 0 4px 15px rgba(255, 65, 108, 0.4);
            border: none;
            cursor: pointer;
            transition: transform 0.2s, box-shadow 0.2s, opacity 0.2s;
            font-family: 'Outfit', sans-serif;
            text-transform: uppercase;
            letter-spacing: 1px;
        }
        .emergency-rb-btn:hover, .emergency-rb-btn.focused {
            transform: translateY(-2px);
            box-shadow: 0 6px 20px rgba(255, 65, 108, 0.6);
            outline: 3px solid white;
        }
        .emergency-rb-btn:active {
            transform: translateY(1px);
        }
        .emergency-rb-btn:disabled {
            opacity: 0.5;
            cursor: wait;
            transform: none;
        }

        .tabs { display: flex; margin-bottom: 30px; background: rgba(0,0,0,0.2); border-radius: 12px; padding: 5px; }
        .tab { flex: 1; padding: 10px; cursor: pointer; border-radius: 8px; transition: 0.3s; font-weight: 500; opacity: 0.6; }
        .tab.active { background: var(--card-border); opacity: 1; color: var(--primary); }

        .game-grid { 
            display: grid; grid-template-columns: repeat(auto-fill, minmax(180px, 1fr)); 
            gap: 20px; margin-top: 20px; padding: 10px; flex-grow: 1; overflow-y: auto;
            align-content: start;
            scrollbar-width: thin; scrollbar-color: var(--card-border) transparent;
            transition: all 0.3s ease;
        }
        .game-item { 
            background: rgba(255,255,255,0.03); border: 1px solid var(--card-border); 
            border-radius: 16px; padding: 12px; text-align: left; transition: 0.3s;
            animation: fadeIn 0.5s ease-out forwards; opacity: 0;
        }
        @keyframes fadeIn { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: translateY(0); } }
        
        .game-item:hover { background: rgba(255,255,255,0.08); transform: translateY(-3px); border-color: var(--primary); }
        .game-item img { width: 100%; border-radius: 10px; margin-bottom: 10px; aspect-ratio: 3/4; object-fit: contain; background: rgba(0,0,0,0.2); box-shadow: 0 5px 15px rgba(0,0,0,0.3); }
        .game-title { font-weight: 600; font-size: 0.95em; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
        .game-sys { font-size: 0.75em; color: var(--primary); text-transform: uppercase; font-weight: 600; opacity: 0.8; }
        
        .search-container { 
            width: 100%; margin-top: 15px; margin-bottom: 5px; display: flex; align-items: center; gap: 10px; 
            padding: 10px; background: rgba(0,0,0,0.2); border-radius: 12px; z-index: 100;
        }
        .search-filter-group { display: flex; gap: 10px; width: 100%; align-items: stretch; }
        select#systemFilter {
            flex: 0 0 auto; width: 130px; background-color: rgba(0,0,0,0.4); border: 1px solid var(--card-border);
            border-radius: 10px; color: white; padding: 0 12px; outline: none; cursor: pointer; font-size: 0.85rem; height: 45px;
        }
        select#systemFilter:focus { border-color: var(--primary); }
        
        .search-input-wrapper { flex: 1; position: relative; }
        #gameSearchInput {
            width: 100%; height: 45px; background: rgba(0,0,0,0.4); border: 1px solid var(--card-border);
            border-radius: 10px; color: white; padding: 0 15px; font-size: 1rem; margin-bottom: 0px;
        }
        #gameSearchInput:focus { border-color: var(--primary); }
        
        .play-btn { 
            margin-top: 12px; padding: 10px; font-size: 0.85em; 
            background: rgba(0, 242, 255, 0.1); color: var(--primary); border: 1px solid var(--primary);
        }
        .play-btn:hover { background: var(--primary); color: #000; }

        /* Stream View */
        #streamView { 
            display: none; width: 100%; height: 100%; border-radius: 16px; 
            overflow: hidden; background: #000; position: relative;
        }
        #listView { flex: 1; overflow: hidden; display: flex; flex-direction: column; }
        #streamIframe { width: 100%; height: 100%; border: none; }
        .stream-overlay { 
            position: absolute; top: 20px; right: 20px; background: rgba(0,0,0,0.6); 
            padding: 8px 15px; border-radius: 20px; display: flex; align-items: center; gap: 8px;
            font-size: 0.8em; border: 1px solid var(--success); color: var(--success);
        }
        .live-dot { width: 8px; height: 8px; background: var(--success); border-radius: 50%; animation: pulse 1.5s infinite; }
        @keyframes pulse { 0% { opacity: 1; } 50% { opacity: 0.3; } 100% { opacity: 1; } }

        /* ESC Long Press Overlay */
        .esc-overlay {
            position: fixed; top: 0; left: 0; width: 100%; height: 100%;
            background: rgba(0,0,0,0.4); backdrop-filter: blur(8px);
            z-index: 99999; display: none; align-items: center; justify-content: center;
            flex-direction: column; color: white; transition: opacity 0.3s; pointer-events: none;
        }
        .esc-ring { width: 150px; height: 150px; position: relative; display: flex; align-items: center; justify-content: center; }
        .esc-ring svg { transform: rotate(-90deg); width: 150px; height: 150px; }
        .esc-circle-bg { fill: none; stroke: rgba(255,255,255,0.1); stroke-width: 8; }
        .esc-circle-fg { 
            fill: none; stroke: var(--primary); stroke-width: 8; 
            stroke-dasharray: 440; stroke-dashoffset: 440; 
            stroke-linecap: round; filter: drop-shadow(0 0 8px var(--primary)); 
            transition: stroke-dashoffset 0.1s linear;
        }
        .esc-text { position: absolute; font-size: 1.5rem; font-weight: bold; text-shadow: 0 0 10px rgba(0,0,0,0.5); }
        .esc-msg { margin-top: 20px; font-size: 1rem; text-transform: uppercase; letter-spacing: 2px; opacity: 0.8; text-align: center; max-width: 80%; }

        /* [BATRUN-FORK] Multiplayer Lobby Overlay — Modern Batrun Style */
        .lobby-overlay {
            position: absolute; top: 0; left: 0; width: 100%; height: 100%;
            background: radial-gradient(ellipse at 30% 20%, rgba(112,0,255,0.2), transparent 50%),
                        radial-gradient(ellipse at 70% 80%, rgba(0,242,255,0.2), transparent 50%),
                        linear-gradient(135deg, #0a0a1a 0%, #0f172a 50%, #0a0a1a 100%);
            z-index: 9998; display: none; flex-direction: column;
            justify-content: center; align-items: center;
            color: white; font-family: 'Outfit', sans-serif; text-align: center;
            backdrop-filter: blur(20px); -webkit-backdrop-filter: blur(20px);
        }
        .lobby-question { font-size: 1.6rem; font-weight: 600; margin-bottom: 25px; letter-spacing: 0.5px; }
        .lobby-choices { display: flex; gap: 20px; margin-top: 10px; }
        .lobby-choice {
            padding: 14px 28px; border-radius: 14px; font-size: 1.1rem; font-weight: 600;
            cursor: pointer; transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
            border: 2px solid transparent; letter-spacing: 0.5px;
            backdrop-filter: blur(10px); -webkit-backdrop-filter: blur(10px);
        }
        .lobby-choice-yes {
            background: rgba(0,255,136,0.1); color: var(--success); border-color: rgba(0,255,136,0.4);
            box-shadow: 0 0 20px rgba(0,255,136,0.1), inset 0 0 20px rgba(0,255,136,0.05);
        }
        .lobby-choice-yes:hover { background: rgba(0,255,136,0.2); border-color: var(--success); transform: scale(1.05); box-shadow: 0 0 30px rgba(0,255,136,0.2); }
        .lobby-choice-no {
            background: rgba(255,0,200,0.1); color: var(--accent); border-color: rgba(255,0,200,0.4);
            box-shadow: 0 0 20px rgba(255,0,200,0.1), inset 0 0 20px rgba(255,0,200,0.05);
        }
        .lobby-choice-no:hover { background: rgba(255,0,200,0.2); border-color: var(--accent); transform: scale(1.05); box-shadow: 0 0 30px rgba(255,0,200,0.2); }
        .lobby-waiting-text {
            font-size: 1.3rem; margin-bottom: 15px; opacity: 0.9;
            text-shadow: 0 0 20px rgba(0,242,255,0.3);
        }

        /* Player Banner (bottom bar) — Modern */
        .lobby-banner {
            position: absolute; bottom: 0; left: 0; width: 100%;
            background: rgba(5,5,16,0.9); backdrop-filter: blur(15px); -webkit-backdrop-filter: blur(15px);
            padding: 12px 25px; display: none; align-items: center;
            gap: 15px; z-index: 9997;
            border-top: 1px solid rgba(0,242,255,0.15);
            box-shadow: 0 -5px 30px rgba(0,0,0,0.5);
        }
        .lobby-banner-label {
            font-size: 0.75rem; color: var(--primary); font-weight: 600;
            text-transform: uppercase; white-space: nowrap; letter-spacing: 2px;
            text-shadow: 0 0 10px rgba(0,242,255,0.3);
        }
        .lobby-player-slot {
            display: flex; align-items: center; gap: 10px;
            padding: 8px 16px; border-radius: 10px;
            background: rgba(255,255,255,0.04); border: 1px solid rgba(255,255,255,0.08);
            transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1); font-size: 0.85rem;
        }
        .lobby-player-slot.ready {
            border-color: rgba(0,255,136,0.5); background: rgba(0,255,136,0.08);
            box-shadow: 0 0 15px rgba(0,255,136,0.1);
        }
        .lobby-player-icon { font-size: 1.1rem; }
        .lobby-player-label { font-weight: 600; color: rgba(255,255,255,0.9); }
        .lobby-player-status { font-weight: 600; font-size: 0.75rem; text-transform: uppercase; letter-spacing: 1px; }
        .lobby-player-status.ready { color: var(--success); text-shadow: 0 0 8px rgba(0,255,136,0.3); }
        .lobby-player-status.not-ready { color: rgba(255,255,255,0.35); }

        /* Lobby spinner — neon style */
        .lobby-overlay .loader-spinner {
            width: 50px; height: 50px; border: 3px solid rgba(0,242,255,0.1);
            border-top: 3px solid var(--primary); border-radius: 50%;
            animation: spin 1s linear infinite; margin: 20px 0;
            box-shadow: 0 0 15px rgba(0,242,255,0.2);
        }

        .stream-control-top-left {
            position: absolute; top: 15px; left: 20px; z-index: 1000;
            display: flex; gap: 10px;
        }
        #btnFullStream {
            background: rgba(0,0,0,0.6); color: white; border: 1px solid rgba(255,255,255,0.4);
            padding: 8px 15px; border-radius: 20px; font-size: 0.8em; cursor: pointer;
            transition: all 0.3s; backdrop-filter: blur(5px);
        }
        #btnFullStream:hover { background: var(--primary); border-color: var(--primary); color: #000; font-weight: bold; }

        #logoutBtn { width: auto; padding: 8px 20px; background: rgba(255,0,50,0.1); border: 1px solid rgba(255,0,50,0.3); color: #ff3c5c; }
        #logoutBtn:hover { background: #ff3c5c; color: white; }

        #message { margin-top: 15px; font-size: 0.9em; padding: 10px; border-radius: 8px; display: none; }
        .msg-error { background: rgba(255,0,0,0.15); color: #ff6b6b; display: block!important; }
        .msg-info { background: rgba(0,242,255,0.15); color: var(--primary); display: block!important; }

        /* Layout changes */
        .header { display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid var(--card-border); padding-bottom: 15px; margin-bottom: 10px; }
        
        .lang-switch { display: flex; gap: 8px; align-items: center; margin-right: 10px; }
        .flag { font-size: 0.85rem; font-weight: bold; cursor: pointer; filter: grayscale(0.6); transition: all 0.3s; background: rgba(255,255,255,0.1); padding: 4px 8px; border-radius: 6px; display: flex; align-items: center; gap: 4px; border: 1px solid transparent; white-space: nowrap; }
        .flag.active { filter: grayscale(0); transform: scale(1.05); background: rgba(255,255,255,0.2); border-color: var(--primary); }
        .header-actions { display: flex; align-items: center; gap: 15px; }

        /* Gamepad Focus & Hide Scroll */
        .focused { outline: 3px solid var(--primary); transform: scale(1.05); z-index: 10; box-shadow: 0 0 20px rgba(0,242,255,0.4); }
        input.focused { transform: none; } /* [BATRUN] Empêche l'input de déborder sur les autres éléments quand il est scale */

        .search-container { transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1), height 0.3s, margin 0.3s, opacity 0.3s, visibility 0.3s; transform-origin: top; overflow: hidden; }
        .search-hidden { transform: translateY(-30px); height: 0; margin-top: 0!important; margin-bottom: 0!important; opacity: 0; pointer-events: none; visibility: hidden; padding-top: 0!important; padding-bottom: 0!important; border:none!important; }
        #publicHeader.search-hidden { transform: translateY(-50px); height: 0; margin-bottom: 0!important; margin-top: 0!important; padding: 0!important; opacity: 0; pointer-events: none; visibility: hidden; border:none!important; }
        
        /* Virtual Keyboard Compact Mode */
        .vk-container.vk-compact .vk-key { padding: 8px 10px; min-width: 32px; font-size: 0.85rem; }

        /* Login Node Selector Layout */
        .auth-layout {
            display: flex;
            gap: 20px;
            align-items: center;
            justify-content: center;
            max-width: 950px;
            max-height: 85vh;
            width: 100%;
            margin: auto;
            padding: 20px;
        }
        
        @media (max-width: 850px) {
            .auth-layout {
                flex-direction: column;
                align-items: center;
                gap: 15px;
                padding: 15px 10px;
            }
            .node-list-container {
                width: 100% !important;
                max-width: 450px;
                height: auto !important;
                max-height: 140px;
                flex-shrink: 0;
            }
            .node-scroll {
                display: flex !important;
                flex-direction: row !important;
                overflow-x: auto !important;
                overflow-y: hidden !important;
                gap: 10px;
                padding: 10px !important;
                mask-image: none !important;
                -webkit-mask-image: none !important;
            }
            .node-tile {
                min-width: 130px;
                margin-bottom: 0 !important;
                flex-shrink: 0;
                padding: 8px 12px !important;
            }
            #authPage {
                justify-content: flex-start !important;
                padding-top: 20px !important;
                height: 100vh;
            }
        }

        .node-list-container {
            width: 180px;
            height: 550px;
            max-height: 80vh;
            background: rgba(0,0,0,0.3);
            border: 1px solid var(--card-border);
            border-radius: 20px;
            display: flex;
            flex-direction: column;
            overflow: hidden;
            box-shadow: 0 10px 30px rgba(0,0,0,0.3);
        }
        .node-scroll {
            flex: 1;
            overflow-y: auto;
            padding: 10px;
            scrollbar-width: thin;
            scrollbar-color: var(--primary) transparent;
        }
        .node-scroll::-webkit-scrollbar { width: 6px; }
        .node-scroll::-webkit-scrollbar-thumb { background: var(--primary); border-radius: 3px; }
        
        .node-tile {
            background: rgba(255,255,255,0.03);
            border: 2px solid transparent;
            border-radius: 12px;
            padding: 12px 8px;
            margin-bottom: 10px;
            cursor: pointer;
            transition: all 0.3s;
            text-align: center;
            position: relative;
        }
        .node-tile:hover, .node-tile.focused {
            background: rgba(56, 189, 248, 0.1);
            border-color: rgba(56, 189, 248, 0.4);
            transform: scale(1.02);
        }
        .node-tile.active {
            border-color: var(--primary);
            background: rgba(56, 189, 248, 0.2);
            box-shadow: 0 0 15px rgba(0, 242, 255, 0.3);
        }
        .node-name { font-size: 0.85rem; font-weight: bold; color: white; display: block; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
        .node-ip { font-size: 0.7rem; opacity: 0.5; font-family: monospace; }
        .node-status { 
            position: absolute; top: 5px; right: 5px; width: 6px; height: 6px; 
            border-radius: 50%; background: #555; 
        }
        .node-status.online { background: var(--success); box-shadow: 0 0 5px var(--success); }
        
        #authContainer.container { 
            max-width: 400px; margin: 0; flex: 1;
        }
        
        @media (max-width: 600px) {
            .auth-layout { flex-direction: column; align-items: center; }
            .node-list-container { width: 100%; height: 100px; flex-direction: row; }
            .node-scroll { 
                display: flex; gap: 10px; flex-direction: row; padding: 10px 15px; 
                mask-image: linear-gradient(to right, transparent, black 10%, black 90%, transparent);
                -webkit-mask-image: linear-gradient(to right, transparent, black 10%, black 90%, transparent);
            }
            .node-tile { margin-bottom: 0; min-width: 100px; flex-shrink: 0; }
        }
        .vk-container.vk-compact .vk-row { gap: 3px; margin-bottom: 3px; }
        .vk-container.vk-compact .vk-key[innerText='SPACE'] { min-width: 100px; }
        .vk-container.vk-compact .vk-key[innerText='BACKSPACE'], .vk-container.vk-compact .vk-key[innerText='ENTER'] { min-width: 60px; font-size: 0.75rem; }
        
        /* Pull Handle for mobile */
        #pullHandle {
            width: 50px; height: 6px; background: rgba(255,255,255,0.2); border-radius: 3px;
            margin: 5px auto; cursor: pointer; display: none; transition: background 0.3s;
            flex-shrink: 0;
        }
        #pullHandle:hover { background: var(--primary); }

        @media (max-width: 900px) and (orientation: landscape) {
            #gameContainer { height: 100vh; width: 100vw; max-width: none; border-radius: 0; }
            .game-grid { grid-template-columns: repeat(auto-fill, minmax(140px, 1fr)); gap: 10px; }
            h1 { font-size: 1.2rem; }
            .search-container { padding: 5px; margin-top: 5px; }
        }

        @media (max-width: 600px) {
            .game-grid { grid-template-columns: repeat(auto-fill, minmax(100px, 1fr)); gap: 10px; padding: 5px; }
            .header { flex-direction: row; gap: 10px; padding: 10px 5px; align-items: center; }
            .header-actions { gap: 8px; flex-shrink: 0; }
            #logoutBtn { padding: 5px 10px; font-size: 0.8rem; }
            .flag { padding: 3px 6px; font-size: 0.75rem; }
            select#systemFilter { width: 90px; font-size: 0.7rem; padding: 0 5px; }
            .modal-content { padding: 20px; }
            input { font-size: 0.9rem; margin-bottom:0; }
            #pullHandle { display: block; }
        }

        /* Portrait adaptations specifically for search bar */
        @media (max-width: 500px) and (orientation: portrait) {
            #searchInnerRow { flex-direction: column !important; gap: 5px !important; }
            #customSystemSelect, #btnOpenRbModal { width: 100% !important; flex: none !important; height: 40px !important; }
            #gameSearchInput { width: 100% !important; flex: none !important; }
        }

        /* Landscape adaptations for smaller screens */
        @media (max-height: 500px) and (orientation: landscape) {
            .game-grid { grid-template-columns: repeat(auto-fill, minmax(130px, 1fr)) !important; gap: 8px !important; }
            .modal-content { max-height: 95vh !important; padding: 15px !important; width: 95% !important; max-width: 800px !important; }
            .modal-media { max-height: 150px !important; margin-bottom: 10px !important; }
            .modal-media img, .modal-media video { max-height: 150px !important; }
            #mdDesc { max-height: 60px !important; font-size: 0.8rem !important; margin-bottom: 10px !important; }
            #mdTitle { font-size: 1.2rem !important; margin-bottom: 5px !important; }
            .btn-launch { padding: 10px !important; font-size: 0.9rem !important; }
            .modal-close { width: 30px !important; height: 30px !important; top: 10px !important; right: 10px !important; }
        }

        /* Game Details Modal */
        #gameModal { position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.85); backdrop-filter: blur(10px); display: none; align-items: center; justify-content: center; z-index: 2000; padding: 20px; opacity: 0; transition: opacity 0.3s; }
        .modal-content { background: var(--card); border: 1px solid var(--card-border); border-radius: 20px; max-width: 600px; width: 100%; max-height: 90vh; overflow-y: auto; padding: 30px; position: relative; box-shadow: 0 15px 40px rgba(0,0,0,0.5); transform: translateY(20px); transition: transform 0.3s; }
        #gameModal.show { opacity: 1; pointer-events: all; }
        #gameModal.show .modal-content { transform: translateY(0); }
        .modal-close { position: absolute; top: 15px; right: 15px; background: rgba(255,255,255,0.1); border: none; color: white; width: 35px; height: 35px; border-radius: 50%; font-size: 1.2rem; cursor: pointer; transition: 0.3s; display:flex; align-items:center; justify-content:center; }
        .modal-close:hover { background: #ff3c5c; }
        .modal-media { width: 100%; border-radius: 12px; margin-bottom: 20px; background: #000; display:flex; align-items:center; justify-content:center; }
    </style>
</head>
<body>
    <div class=""bg-glow""></div>

    <div id=""authPage"" style=""display:flex; flex-direction:column; align-items:center; justify-content:flex-start; min-height:100vh; width:100%; position:relative; z-index:10; overflow-y: auto;"">
        
        <div class=""auth-layout"">
            <!-- Node Selector (Left Sidebar) -->
            <div id=""nodeListContainer"" class=""node-list-container"">
                <div id=""nodeScroll"" class=""node-scroll"">
                    {GetStaticNodeHtml()}
                </div>
            </div>

            <div class=""container"" id=""authContainer"">
                <h1 id=""viewTitle"" style=""margin-bottom: 5px;"">BATRUN ACCESS</h1>
                <p id=""txtMachineTarget"" style=""font-size: 0.8rem; color: var(--primary); margin-bottom: 25px; opacity: 0.8; font-weight: bold; text-transform: uppercase;"">LOCAL MACHINE</p>
        <div class=""lang-switch"" style=""margin-bottom: 20px;"">
            <span class=""flag"" id=""flag-fr-auth"" onclick=""setLanguage('fr')""><img src=""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 3 2'%3E%3Crect width='3' height='2' fill='%23ED2939'/%3E%3Crect width='2' height='2' fill='%23fff'/%3E%3Crect width='1' height='2' fill='%23002395'/%3E%3C/svg%3E"" style=""width:16px; height:11px; border-radius:1px;"" alt=""FR""> FR</span>
            <span class=""flag"" id=""flag-en-auth"" onclick=""setLanguage('en')""><img src=""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 60 30'%3E%3CclipPath id='t'%3E%3Cpath d='M30,15h30v15zv15H0zH0V0zV0h30z'/%3E%3C/clipPath%3E%3Cpath d='M0,0v30h60V0z' fill='%23012169'/%3E%3Cpath d='M0,0 60,30M60,0 0,30' stroke='%23fff' stroke-width='6'/%3E%3Cpath d='M0,0 60,30M60,0 0,30' clip-path='url(%23t)' stroke='%23C8102E' stroke-width='4'/%3E%3Cpath d='M30,0v30M0,15h60' stroke='%23fff' stroke-width='10'/%3E%3Cpath d='M30,0v30M0,15h60' stroke='%23C8102E' stroke-width='6'/%3E%3C/svg%3E"" style=""width:16px; height:11px; border-radius:1px;"" alt=""EN""> EN</span>
        </div>
        <div class=""tabs"" style=""display: {AUTH_TABS_DISPLAY};"">
            <div class=""tab active"" id=""tabLogin"" onclick=""switchTab('login')"">Connexion</div>
            <div class=""tab"" id=""tabRegister"" onclick=""switchTab('register')"" style=""display: {AUTH_REGISTER_DISPLAY};"">Inscription</div>
        </div>
        <form id=""authForm"" method=""POST"" action=""/connect"" onsubmit=""return handleAuthSubmit(event);"">
            <div id=""loginFields"" style=""display: {AUTH_LOGIN_DISPLAY};"">
                <div class=""input-group"">
                    <label for=""username"" style=""display: none;"">Utilisateur</label>
                    <input type=""text"" id=""username"" name=""username"" autocomplete=""username"" placeholder=""Nom d'utilisateur"" required>
                </div>
                <div class=""input-group"">
                    <label for=""password"" style=""display: none;"">Mot de passe</label>
                    <input type=""password"" id=""password"" name=""password"" autocomplete=""current-password"" placeholder=""Mot de passe"" required>
                </div>
            </div>

            <div id=""noLoginFields"" style=""display: {AUTH_NO_LOGIN_DISPLAY}; margin-bottom: 25px;"">
                <button type=""button"" id=""btnGuestStart"" onclick=""submitAuth('Guest', 'Guest')"" style=""font-size: 1.5rem; padding: 25px; height: auto; border: 2px solid var(--primary); background: rgba(0, 242, 255, 0.1); color: var(--primary);"">{AUTH_GUEST_START_LABEL}</button>
            </div>
            <!-- Virtual Keyboard Login -->
            <div id=""authVirtualKeyboard"" class=""virtual-keyboard""></div>
            <button id=""submitBtn"" type=""submit"" style=""display: {AUTH_SUBMIT_DISPLAY}; width: 100%;"">Se connecter</button>
        </form>
        <div id=""message""></div>
        
        {AUTH_ADMIN_LINK_BLOCK}
            </div>
        </div>
    </div>

    <script>
        let isLogin = true;
        let requiresLogin = {REQUIRES_LOGIN_JS};
        let isExternalClient = {IS_EXTERNAL_JS};
        let apiBase = '';
        const localApiBase = '';
        let selectedNodeObj = null;
        let selectedNodeName = null;
        let _lobbySessionId = 'sess_' + Math.random().toString(36).substr(2, 9) + '_' + Date.now();
        let _activeInput = null;
        let _gpActive = false;

        let _activeKb = null;
        let _vkShift = false;
        let _vkFocusedIndex = 0;
        let _gpContext = 'main';

        const KEYBOARD_LAYOUTS = {
            fr: [ // AZERTY
                ['1','2','3','4','5','6','7','8','9','0'],
                ['A','Z','E','R','T','Y','U','I','O','P'],
                ['Q','S','D','F','G','H','J','K','L','M'],
                ['MAJ','W','X','C','V','B','N','?','!','DEL'],
                ['ESPACE', 'EFFACER', 'OK']
            ],
            en: [ // QWERTY
                ['1','2','3','4','5','6','7','8','9','0'],
                ['Q','W','E','R','T','Y','U','I','O','P'],
                ['A','S','D','F','G','H','J','K','L',';'],
                ['MAJ','Z','X','C','V','B','N','M','?','DEL'],
                ['SPACE', 'CLEAR', 'OK']
            ]
        };

        // EN: Handle token passed via URL (for post-login redirection)
        // FR: Gerer le token passe via URL (pour redirection post-login)
        const urlParams = new URLSearchParams(window.location.search);
        
        // EN: Handle force logout flag to break redirect loops / FR: Gérer le flag de déconnexion forcée pour casser les boucles
        if (urlParams.get('logout') === '1') {
            console.warn('[BatRun] Force logout requested via URL. Clearing tokens.');
            localStorage.removeItem('batrun_token');
            document.cookie = 'batrun_token=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
            window.history.replaceState({}, document.title, window.location.pathname);
        }

        const urlToken = urlParams.get('token');
        if (urlToken) {
            localStorage.setItem('batrun_token', urlToken);
            window.history.replaceState({}, document.title, window.location.pathname);
        }
        const _loginError = urlParams.get('login_error');
        if (_loginError) {
            window.history.replaceState({}, document.title, window.location.pathname);
        }

        const publicTranslations = {
            fr: {
                tabLogin: 'Connexion',
                tabRegister: 'Inscription',
                username: ""Nom d'utilisateur"",
                password: ""Mot de passe"",
                btnConnect: 'Se connecter',
                btnRegister: ""S'inscrire"",
                adminLink: '⚙️ Accès Administrateur / Opérateur',
                msgPending: ""Compte en attente de validation par l'opérateur."",
                msgInvalid: ""Identifiants incorrects"",
                msgNetErr: ""Impossible de joindre le serveur Arcade."",
                viewTitleIdle: ""🕹️ BatRun Connect"",
                viewTitleStream: ""📺 Streaming Actif"",
                btnLogout: ""Sortir"",
                txtLoadingGames: ""Initialisation des jeux..."",
                loadingWait: ""⌛ Chargement..."",
                noGames: ""Aucun jeu trouvé."",
                btnStart: ""DEMARRER"",
                btnGuestStart: ""DÉMARRER"",
                playBusy: ""Un jeu est déjà en cours"",
                // [BATRUN-FORK-v4]: Join existing session button
                btnJoinSession: ""REJOINDRE"",
                joinSessionHint: ""Session en cours :"",
                txtLiveStream: ""LIVE STREAM"",
                btnStopGame: ""Quitter le jeu"",
                btnFullscreen: ""Plein Écran"",
                errLaunch: ""Le serveur ne peut pas lancer le jeu pour le moment."",
                errConn: ""Erreur de connexion."",
                searchPlaceholder: ""Rechercher un jeu..."",
                sysFilterAll: ""TOUS LES SYSTEMES"",
                sysFilterUnknown: ""Inconnu"",
                sysModalTitle: ""SÉLECTIONNER UN SYSTÈME"",
                msgConnecting: ""Connexion au flux vidéo..."",
                msgLoadingRb: ""Lancement interface RetroBat..."",
                rbModalTitle: ""Lancer RetroBat ?"",
                rbModalDesc: ""Vous êtes sur le point de lancer l'interface complète de RetroBat. Confirmer ?"",
                rbCancel: ""Annuler"",
                rbLaunch: ""Lancer"",
                btnRb: ""RETROBAT"",
                btnFullscreen: ""Plein Écran"",
                txtLiveStream: ""EN DIRECT"",
                btnStopGame: ""Quitter le jeu"",
                escHold: ""Maintenez ÉCHAP pour quitter"",
                escExit: ""Sortie"",
                msgPressAnyButton: '🎮 Appuyez sur un bouton de la manette pour démarrer 🎮<br><br><span style=""font-size:0.5em; opacity:0.5;"">(Ou Clic/Entrée)</span>',
                msgLaunching: ""Lancement du jeu..."",
                lobbyWaiting: ""En attente de joueurs..."",
                lobbyReady: ""START = PRÊT"",
                lobbySolo: ""X = CONTINUER SEUL"",
                lobbyPlayerReady: ""PRÊT"",
                lobbyPlayerNotReady: ""Appuyez START"",
                lobbyConfirmLaunch: ""Tous prêts ! START pour lancer le jeu"",
                lobbyP1Confirm: ""Joueur 1, confirmez avec START"",
                lobbyPlayerJoined: ""Joueur {n} connecté"",
                lobbyActive: ""Lobby en cours"",
                lobbyLaunching: ""Lancement du jeu en cours..."",
                lobbyJoinBannerTitle: ""Lobby en cours"",
                lobbyJoinBtn: ""Rejoindre"",
                lobbyAllReady: ""Tous les joueurs sont prêts !"",
                lobbyStartLaunch: ""START = LANCER LE JEU"",
                lobbyWaitingPlayers: ""Attente de confirmation : {ready}/{total} prêts"",
                lobbyStartSolo: ""START = PRÊT"",
                emergencyHoldMsg: ""SELECT + START = FERMER LA SESSION"",
                emergencyConfirmTitle: ""FERMETURE DE SESSION"",
                emergencyConfirmDesc: ""Voulez-vous forcer la fermeture de la session ? Tous les joueurs seront déconnectés."",
                emergencyCancel: ""Annuler"",
                emergencyConfirm: ""CONFIRMER"",
                lobbyStopBtn: ""Arrêt forcé lobby"",
                errGameServerDown: ""Le serveur de jeux est hors ligne."",
                btnStartGameServer: ""DÉMARRER LE SERVEUR DE JEUX""
                },
            en: {
                tabLogin: 'Login',
                tabRegister: 'Register',
                username: 'Username',
                password: 'Password',
                btnConnect: 'Connect',
                btnRegister: 'Register',
                adminLink: '⚙️ Operator / Admin Access',
                msgPending: 'Account pending operator approval.',
                msgInvalid: 'Invalid credentials',
                msgNetErr: 'Server unreachable.',
                viewTitleIdle: ""🕹️ BatRun Connect"",
                viewTitleStream: ""📺 Streaming Active"",
                btnLogout: ""Log out"",
                searchPlaceholder: ""Search for a game... "",
                btnFullscreen: ""Fullscreen"",
                viewDetails: ""Details"",
                btnStart: ""START"",
                btnGuestStart: ""START"",
                playBusy: ""IN PROGRESS"",
                // [BATRUN-FORK-v4]: Join existing session button
                btnJoinSession: ""JOIN"",
                joinSessionHint: ""Current session:"",
                loadingWait: ""Loading list..."",
                noGames: ""No games found."",
                errConn: ""Connection error."",
                sysFilterAll: ""ALL SYSTEMS"",
                sysFilterUnknown: ""UNKNOWN"",
                sysModalTitle: ""SELECT A SYSTEM"",
                msgConnecting: ""Connecting to video stream..."",
                msgLoadingRb: ""Launching RetroBat interface..."",
                rbModalTitle: ""Launch RetroBat?"",
                rbModalDesc: ""You are about to launch the full RetroBat interface. Confirm?"",
                rbCancel: ""Cancel"",
                rbLaunch: ""Launch"",
                btnRb: ""RETROBAT"",
                txtLiveStream: ""LIVE STREAM"",
                btnStopGame: ""Quit Game"",
                escHold: ""Hold ESC to exit"",
                escExit: ""Exit"",
                msgPressAnyButton: '🎮 Press any game controller button to start 🎮<br><br><span style=""font-size:0.5em; opacity:0.5;"">(Or Click/Enter)</span>',
                msgLaunching: ""Launching game..."",
                lobbyWaiting: ""Waiting for players..."",
                lobbyReady: ""START = READY"",
                lobbySolo: ""X = PLAY SOLO"",
                lobbyPlayerReady: ""READY"",
                lobbyPlayerNotReady: ""Press START"",
                lobbyConfirmLaunch: ""All ready! Press START to launch"",
                lobbyP1Confirm: ""Player 1, confirm with START"",
                lobbyPlayerJoined: ""Player {n} joined"",
                lobbyActive: ""Lobby in progress"",
                lobbyLaunching: ""Launching game..."",
                lobbyJoinBannerTitle: ""Lobby in progress"",
                lobbyJoinBtn: ""Join"",
                lobbyAllReady: ""All players are ready!"",
                lobbyStartLaunch: ""START = LAUNCH GAME"",
                lobbyWaitingPlayers: ""Waiting for confirmation: {ready}/{total} ready"",
                lobbyStartSolo: ""START = READY"",
                emergencyHoldMsg: ""SELECT + START = CLOSE SESSION"",
                emergencyConfirmTitle: ""CLOSE SESSION"",
                emergencyConfirmDesc: ""Force close the current session? All players will be disconnected."",
                emergencyCancel: ""Cancel"",
                emergencyConfirm: ""CONFIRM"",
                lobbyStopBtn: ""Stop Session"",
                errGameServerDown: ""Game server is offline."",
                btnStartGameServer: ""START GAME SERVER""
                }
        };

        // EN: Restore language from localStorage, fallback to browser language
        // FR: Restaurer la langue depuis localStorage, sinon langue du navigateur
        let currentLang = localStorage.getItem('batrun_lang') || (navigator.language.startsWith('fr') ? 'fr' : 'en');
        const isMobile = /iPhone|iPad|iPod|Android/i.test(navigator.userAgent);

        // EN: Set language for auth page elements / FR: Definir la langue des elements d'authentification
        function setLanguage(lang) {
            currentLang = lang;
            // EN: Persist language choice across pages and refreshes
            // FR: Persister le choix de langue entre les pages et les rafraichissements
            localStorage.setItem('batrun_lang', lang);
            const t = publicTranslations[lang];
            // EN: Auth page elements / FR: Elements de la page d'authentification
            const tabL = document.getElementById('tabLogin'); if (tabL) tabL.innerText = t.tabLogin;
            const tabR = document.getElementById('tabRegister'); if (tabR) tabR.innerText = t.tabRegister;
            const uField = document.getElementById('username'); if (uField) uField.placeholder = t.username;
            const pField = document.getElementById('password'); if (pField) pField.placeholder = t.password;
            const subBtn = document.getElementById('submitBtn');
            if (subBtn) subBtn.innerText = isLogin ? t.btnConnect : t.btnRegister;
            const gBtn = document.getElementById('btnGuestStart');
            if (gBtn) gBtn.innerText = t.btnGuestStart || 'START';
            // EN: Flag highlights (auth page uses -auth suffix) / FR: Drapeaux actifs (page auth utilise le suffixe -auth)
            document.querySelectorAll('.flag').forEach(f => f.classList.remove('active'));
            const fl = document.getElementById('flag-' + lang + '-auth'); if (fl) fl.classList.add('active');
        }

        function switchTab(tab) {
            isLogin = tab === 'login';
            document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
            event.target.classList.add('active');
            document.getElementById('submitBtn').innerText = isLogin ? publicTranslations[currentLang].btnConnect : publicTranslations[currentLang].btnRegister;
            
            // [BATRUN-FIX]: Help browser credential manager distinguish between login and registration
            const passInput = document.getElementById('password');
            if (passInput) passInput.autocomplete = isLogin ? 'current-password' : 'new-password';
            
            const msg = document.getElementById('message');
            msg.innerText = '';
            msg.className = '';
        }


        function getRelayUrl(path) {
            // [BATRUN-FIX]: apiBase already contains the relay path and target param.
            // FR: apiBase contient déjà le chemin /api/relay et le paramètre target.
            // On se contente de concaténer le chemin demandé.
            if (apiBase) {
                return apiBase + path;
            }
            return path;
        }

        // [BATRUN-CRED] EN: Credential detection via URL change: /connect (login) → POST /connect → redirect /cloud
        // This is the universal pattern recognized by ALL browser password managers.
        // FR: Détection des identifiants via changement d'URL : /connect (login) → POST /connect → redirect /cloud

        function handleAuthSubmit(event) {
            const u = document.getElementById('username')?.value || '';
            const p = document.getElementById('password')?.value || '';

            // EN: On /connect page, local login — let the browser see the real POST → /cloud redirect
            // FR: Sur la page /connect, login local — laisser le navigateur voir le vrai POST → redirect /cloud
            const onConnectPage = window.location.pathname === '/connect' || window.location.pathname === '/';
            if (onConnectPage && !apiBase && isLogin && u && p) {
                return true; // EN: Native submit — DO NOT preventDefault
            }

            // EN: Remote machine (relay) or Guest or registration — use fetch
            event.preventDefault();
            submitAuth();
            return false;
        }

        async function submitAuth(uOverride = null, pOverride = null) {
            const u = uOverride || document.getElementById('username').value;
            const p = pOverride || document.getElementById('password').value;
            const endpoint = isLogin ? '/api/public/login' : '/api/public/register';
            const msg = document.getElementById('message');
            const t = publicTranslations[currentLang];

            try {
                const res = await fetch(getRelayUrl(endpoint), {
                    method: 'POST',
                    headers: {'Content-Type': 'application/json'},
                    body: JSON.stringify({username: u, password: p, deviceId: _lobbySessionId})
                });
                const data = await res.json();
                
                if (res.ok) {
                    if (data.token) {
                        localStorage.setItem('batrun_token', data.token);
                        document.cookie = 'batrun_token=' + data.token + '; path=/; max-age=86400; SameSite=Lax';

                        // [BATRUN-CRED] EN: Ask browser to save credentials via Credential Management API
                        // FR: Demander au navigateur de mémoriser les identifiants (API Credential Management)
                        if (window.PasswordCredential && u && p) {
                            try {
                                const cred = new PasswordCredential({
                                    id: u,
                                    password: p,
                                    name: u
                                });
                                navigator.credentials.store(cred);
                            } catch(ce) { console.warn('[BatRun] credentials.store failed:', ce); }
                        }

                        // [BATRUN-IRON]: Direct IP Navigation Strategy (Redirection after successful login)
                        // EN: If destination is remote, switch entire browser to that IP to ensure Moonlight-Web compatibility
                        // FR: On abandonne le HubMode relay: on redirige totalement le navigateur vers l'IP:Port de la machine.
                        if (selectedNodeObj && !selectedNodeObj.isLocal) {
                            const useIp = selectedNodeObj.publicIp || selectedNodeObj.ipAddress || selectedNodeObj.ip || '';
                            const rawIp = String(useIp).split('/')[0].split('?')[0].split(':')[0];
                            const usePort = selectedNodeObj.port || selectedNodeObj.apiPort || 4321;
                            const redirectUrl = `http://${rawIp}:${usePort}/cloud?token=${data.token}`;
                            console.log('[BatRun] Redirecting to direct target IP after login: ' + redirectUrl);
                            window.location.href = redirectUrl;
                            return;
                        }

                        showMainUI();
                    } else if (data.status === 'pending') {
                        msg.innerText = t.msgPending;
                        msg.className = 'msg-info';
                    }
                } else {
                    msg.innerText = data.error === 'Invalid credentials' && currentLang === 'fr' ? t.msgInvalid : (data.error || t.msgInvalid);
                    msg.className = 'msg-error';
                }
            } catch (e) {
                msg.innerText = t.msgNetErr;
                msg.className = 'msg-error';
            }
        }

        // EN: Redirect to cloud page after successful login (NEVER show games on connect page)
        // FR: Rediriger vers la page cloud apres connexion reussie (JAMAIS de jeux sur la page connect)
        function showMainUI() {
            window.location.replace('/cloud');
        }

        function buildVirtualKeyboard() {
            if (!_activeKb) return;
            const kb = _activeKb;
            const layout = KEYBOARD_LAYOUTS[currentLang] || KEYBOARD_LAYOUTS['en'];
            kb.innerHTML = '';
            layout.forEach(row => {
                const rowDiv = document.createElement('div');
                rowDiv.className = 'vk-row';
                row.forEach(key => {
                    const btn = document.createElement('button');
                    btn.type = 'button'; // EN: Prevent form submission on key press / FR: Empêcher la soumission du formulaire lors de l'appui sur une touche
                    btn.className = 'vk-key';
                    const displayKey = (_vkShift && key.length === 1) ? key.toLowerCase() : key;
                    btn.innerText = displayKey;
                    if (key === 'ESPACE' || key === 'SPACE') btn.style.flex = '3';
                    if (key === 'OK' || key === 'EFFACER' || key === 'CLEAR') btn.style.flex = '2';
                    btn.addEventListener('click', () => handleVkKey(key));
                    rowDiv.appendChild(btn);
                });
                kb.appendChild(rowDiv);
            });
            _vkFocusedIndex = 0;
            const keys = kb.querySelectorAll('.vk-key');
            if(keys[0]) keys[0].classList.add('focused');
        }

        function handleVkKey(key) {
            const inputTarget = _activeInput || document.getElementById('gameSearchInput');
            if (!inputTarget) return;

            if (key === 'DEL') {
                inputTarget.value = inputTarget.value.slice(0, -1);
            } else if (key === 'ESPACE' || key === 'SPACE') {
                inputTarget.value += ' ';
            } else if (key === 'EFFACER' || key === 'CLEAR') {
                inputTarget.value = '';
            } else if (key === ""OK"") {
                closeVirtualKeyboard();
                if (inputTarget.id === ""gameSearchInput"") {
                    searchGames();
                } else if (inputTarget.id === ""username"") {
                    // EN: Tab to password / FR: Passer au mot de passe
                    const pw = document.getElementById('password');
                    if (pw) {
                        _activeInput = pw;
                        pw.focus();
                        buildVirtualKeyboard();
                    }
                } else if (inputTarget.id === ""password"") {
                    // EN: Final submit / FR: Validation finale
                    submitAuth();
                }
                return;
            } else if (key === 'MAJ') {
                _vkShift = !_vkShift;
                buildVirtualKeyboard();
                return;
            } else {
                inputTarget.value += _vkShift ? key.toLowerCase() : key;
            }
            
            // Dispatch input event to trigger any linked logic (like debounced search)
            inputTarget.dispatchEvent(new Event('input', { bubbles: true }));
        }

        function openVirtualKeyboard() {
            // [BATRUN-FIX]: Block VK when auth page is visible BUT there are no login fields displayed (Guest mode).
            // Uses DOM state instead of requiresLogin variable, because external IPs override requiresLogin.
            // FR: Bloquer le clavier virtuel si la page login est visible mais que les champs sont masqués.
            const _authPage = document.getElementById('authPage');
            const _loginFields = document.getElementById('loginFields');
            if (_authPage && _authPage.style.display !== 'none' && (!_loginFields || _loginFields.style.display === 'none')) return;

            // EN: On mobile, the system keyboard is usually better FOR TOUCH.
            // EN: BUT for Gamepad users on mobile, we NEED the Virtual Keyboard.
            // FR: Sur mobile, le clavier système est préférable pour le TACTILE.
            // FR: MAIS pour les utilisateurs MANETTE sur mobile, nous avons BESOIN du clavier virtuel.
            if (isMobile && !_gpActive) return;

            const inputTarget = _activeInput || document.getElementById('gameSearchInput');
            if (!inputTarget) return;
            // [BATRUN-FIX]: Don't open VK if the target input is hidden (e.g. auth inputs when requiresLogin=false)
            // FR: Ne pas ouvrir le clavier si le champ cible est masqué
            if (inputTarget.offsetParent === null) return;

            // Determine which KB container to use
            const gameContainer = document.getElementById('gameContainer');
            if (gameContainer && gameContainer.style.display === 'flex') {
                _activeKb = document.getElementById('searchVirtualKeyboard');
            } else {
                _activeKb = document.getElementById('authVirtualKeyboard');
            }

            if (!_activeKb) return;

            // EN: Apply compact style for mobile or small screens
            if (isMobile || window.innerHeight < 600) {
                _activeKb.classList.add('vk-compact');
            } else {
                _activeKb.classList.remove('vk-compact');
            }

            buildVirtualKeyboard();
            _activeKb.style.display = 'flex';
            _gpContext = 'keyboard';
            try { if (_activeInput) _activeInput.focus(); } catch(e) {}
            
            // Scroll to ensure search bar + keyboard are visible
            setTimeout(() => {
                _activeKb?.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
            }, 50);
        }

        function closeVirtualKeyboard() {
            if (_activeKb) _activeKb.style.display = 'none';
            _activeKb = null;
            _vkShift = false;
            // Need to return to auth if we were in auth
            const auth = document.getElementById('authContainer');
            if (auth && auth.style.display !== 'none') {
                _gpContext = 'auth';
            } else {
                _gpContext = 'main';
            }
        }

        // EN: Keyboard escape closes modal/keyboard/system / FR: Echap ferme modal/clavier/sélecteur

        document.addEventListener('focusin', function(e) {
            if (e.target.tagName === 'INPUT' && (e.target.type === 'text' || e.target.type === 'password')) {
                _activeInput = e.target;
                // If using Gamepad, open keyboard when selecting an input
                if (_gpActive) {
                    e.target.setAttribute('inputmode', 'none'); // EN: Block native keyboard / FR: Bloquer le clavier natif
                    openVirtualKeyboard();
                } else {
                    e.target.removeAttribute('inputmode'); // EN: Allow native keyboard / FR: Autoriser le clavier natif
                }
            }
        });

        // Allow click explicitly if needed
        document.getElementById('gameSearchInput')?.addEventListener('click', function() {
            _activeInput = this;
            this.removeAttribute('inputmode');
            // Native keyboard pops up properly. Do not force VK on mouse/touch clicks.
        });


        function logout() {
            localStorage.removeItem('batrun_token');
            document.cookie = 'batrun_token=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
            window.location.href = '/connect';
        }

        function getCookie(name) {
            const value = '; ' + document.cookie;
            const parts = value.split('; ' + name + '=');
            if (parts.length === 2) return parts.pop().split(';').shift();
            return null;
        }

        // EN: Route handling - if token exists, validate it before redirecting to /cloud
        // FR: Gestion du routage - si token existe, le valider avant de rediriger vers /cloud
        let batrunToken = localStorage.getItem('batrun_token') || getCookie('batrun_token');
        if (batrunToken) {
            // EN: Validate token with server to prevent redirect loop with stale tokens
            // FR: Valider le token avec le serveur pour éviter la boucle de redirection avec des tokens périmés
            fetch('/api/public/status?token=' + encodeURIComponent(batrunToken))
                .then(res => {
                    if (res.ok) {
                        localStorage.setItem('batrun_token', batrunToken);
                        window.location.replace('/cloud');
                    } else {
                        // EN: Token is invalid/expired - clear it and stay on login page
                        // FR: Token invalide/expiré - le supprimer et rester sur la page de connexion
                        console.warn('[BatRun] Stale token detected on /connect. Clearing.');
                        localStorage.removeItem('batrun_token');
                        document.cookie = 'batrun_token=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
                    }
                })
                .catch(() => {
                    // EN: Network error - clear token to be safe
                    // FR: Erreur réseau - supprimer le token par sécurité
                    localStorage.removeItem('batrun_token');
                    document.cookie = 'batrun_token=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
                });
        }

        setLanguage(currentLang);

        async function loadNodes() {
            const container = document.getElementById('nodeScroll');
            if (!container) return;
            try {
                const res = await fetch('/api/public/nodes');
                if (!res.ok) return;
                const nodes = await res.json();
                if (nodes && Array.isArray(nodes) && nodes.length > 0) {
                    const currentTiles = container.querySelectorAll('.node-tile');
                    if (currentTiles.length !== nodes.length) {
                        renderNodes(nodes);
                    } else {
                        updateNodeStatus(nodes);
                    }
                }
            } catch (e) { }
        }

        function renderNodes(nodes) {
            const container = document.getElementById('nodeScroll');
            if (!container) return;
            container.innerHTML = '';
            nodes.forEach(m => {
                const tile = document.createElement('div');
                tile.className = 'node-tile' + (selectedNodeName === m.name || (!selectedNodeName && m.isLocal) ? ' active' : '');
                tile.dataset.name = m.name;
                tile.dataset.requiresLogin = m.requiresLogin ? 'true' : 'false';
                tile.onclick = () => selectNode(m, tile);
                tile.innerHTML = `
                    <div class=""node-status ${m.isOnline ? 'online' : ''}""></div>
                    <div class=""node-name"" style=""font-weight:bold; font-size:0.9rem; overflow:hidden; text-overflow:ellipsis; white-space:nowrap;"">${m.name || 'Arcade'}</div>
                    <div class=""node-ip"" style=""font-size:0.7rem; opacity:0.6;"">${m.displayIp || (m.isLocal ? 'LOCAL' : m.ip)}</div>
                `;
                container.appendChild(tile);
                // EN: Only auto-select local if nothing is currently selected
                // FR: Auto-sélectionner la locale uniquement si rien n'est sélectionné
                const hasActive = container.querySelector('.node-tile.active') !== null;
                if (m.isLocal && !hasActive && !apiBase) selectNode(m, tile, true);
            });
        }

        function updateNodeStatus(nodes) {
            const container = document.getElementById('nodeScroll');
            if (!container) return;
            nodes.forEach(m => {
                const tile = Array.from(container.querySelectorAll('.node-tile')).find(t => t.dataset.name === m.name);
                if (tile) {
                    const status = tile.querySelector('.node-status');
                    if (status) { status.className = 'node-status ' + (m.isOnline ? 'online' : ''); }
                }
            });
        }

function selectNode(m, nodeEl = null, initial = false) {
closeVirtualKeyboard();
// [BATRUN-FIX]: Do NOT null _activeInput here; focusin listener will reassign it.
// FR: Ne PAS nullifier _activeInput ici; le listener focusin le réassignera.
// _activeInput = null;
_gpActive = false;
            const tiles = document.querySelectorAll('.node-tile');
            tiles.forEach(t => t.classList.remove('active'));
            if (nodeEl) {
                nodeEl.classList.add('active');
                if (m) selectedNodeName = m.name;
            } else if (m && m.name) {
                selectedNodeName = m.name;
                const target = Array.from(tiles).find(t => t.dataset.name === m.name);
                if (target) target.classList.add('active');
            }

            if (!m) return;
            selectedNodeObj = m; 

            // [BATRUN-IRON]: Direct Navigation Swap
            if (!m.isLocal && m.ip !== '127.0.0.1' && m.ip !== window.location.hostname) {
                var curHost = window.location.hostname;
                var usePort = m.port || m.apiPort || 4321;
                
                var isLocalHost = function(h) { 
                    return h === 'localhost' || h === '127.0.0.1' || h.indexOf('192.168.') === 0 || h.indexOf('10.') === 0 || h.indexOf('172.') === 0; 
                };
                
                var targetUrl = '';
                if (!isLocalHost(curHost)) {
                    targetUrl = 'http://' + curHost + ':' + usePort + '/';
                } else {
                    targetUrl = 'http://' + (m.ip || '') + ':' + usePort + '/';
                }

                if (targetUrl && window.location.href.indexOf(targetUrl) !== 0) {
                    console.log('[BatRun] Swapping to target node: ' + targetUrl);
                    window.location.href = targetUrl;
                    return; 
                }
            }

            // [BATRUN-HUB]: Update admin link to point to relay target for remote machines
            // FR: Mettre à jour le lien admin pour pointer vers le relay target pour les machines distantes
            const lnkAdmin = document.getElementById('lnkAdmin');
            
            if (m.isLocal || m.ip === '127.0.0.1' || m.ip === window.location.hostname) {
                apiBase = localApiBase;
                const targetText = document.getElementById('txtMachineTarget');
                if (targetText) targetText.innerText = 'LOCAL MACHINE';
                // EN: Reset admin link to local admin
                if (lnkAdmin) lnkAdmin.href = '/admin';
            } else {
                // [BATRUN-MOD]: Atomic IP Sanitization
                // EN: Strictly keep ONLY host piece, stripping any relay garbage from previous sessions/cached data
                let rawIp = String(m.ip || '');
                rawIp = rawIp.split('/')[0].split('?')[0].split(':')[0];

                // [BATRUN-HUB]: Use machine name as alias instead of raw IP:port for fetching status via relay
                // EN: We keep the relay ONLY for API calls on this page to avoid CORS issues.
                // FR: On conserve le relai UNIQUEMENT pour les appels API sur cette page (évite CORS).
                apiBase = `/api/relay?target=${encodeURIComponent(m.name)}&path=`;
                const targetText = document.getElementById('txtMachineTarget');
                if (targetText) targetText.innerText = m.name;
                
                // [BATRUN-IRON]: Update admin link to point directly to remote machine
                // FR: Mettre à jour le lien admin pour pointer directement vers la machine distante
                const useIp = m.publicIp || m.ipAddress || m.ip || '';
                const cleanIp = String(useIp).split('/')[0].split('?')[0].split(':')[0];
                const usePort = m.port || m.apiPort || 4321;
                if (lnkAdmin) lnkAdmin.href = `http://${cleanIp}:${usePort}/admin`;
            }

            // [BATRUN-IRON]: Cross-verification with target status (Fallback/non-ML)
            fetch(apiBase + '/api/public/status')
                    .then(r => r.json())
                    .then(data => {
                        if (data && data.requiresLogin !== undefined) {
                            const actualReq = data.requiresLogin;
                            const currentLocal = (requiresLogin === 'true' || requiresLogin === true || requiresLogin === 1);
                            if (actualReq !== currentLocal) {
                                console.log('[BatRun] Security sync fix: remote requiresLogin=' + actualReq);
                                updateAuthUI(actualReq); // [BATRUN-FIX]: Now correctly overrides global state
                            }
                        }
            }).catch(e => console.warn('[BatRun] Status sync failed:', e));
            
            if (!initial) {
                const msg = document.getElementById('message');
                if (msg) msg.style.display = 'none';
                systemsLoaded = false;
                currentGamesList = [];
                const gc = document.getElementById('publicGamesList');
                if (gc) gc.innerHTML = '';
                const userInput = document.getElementById('username');
                const passInput = document.getElementById('password');
                if (userInput) userInput.value = '';
                if (passInput) passInput.value = '';
            }

            // [BATRUN-MOD]: Remote login visibility detection
            if (m && m.requiresLogin !== undefined) {
                requiresLogin = m.requiresLogin;
                requiresLogin = nodeEl.dataset.requiresLogin === 'true';
            }
            updateAuthUI();

            if (!initial && _gpActive && !requiresLogin && document.getElementById('authContainer') && document.getElementById('authContainer').style.display !== 'none') {
                setTimeout(() => {
                    const btnG = document.getElementById('btnGuestStart');
                    if (btnG) btnG.focus();
                }, 50);
            }
        }
        window.selectNode = selectNode;

        function updateAuthUI(overReq = null) {
            // [BATRUN-FIX]: Handle priority override from async security check
            if (overReq !== null) {
                requiresLogin = overReq;
            }
            
            // [BATRUN-MOD]: External IPs MUST ALWAYS see login fields. 
            // We ensure priority by checking isExternalClient first and forcing it.
            // FR: Les IPs externes DOIVENT TOUJOURS voir les champs de login.
            const _isExt = (isExternalClient === true || isExternalClient === 'true');
            const _isReqVal = (requiresLogin === true || requiresLogin === 'true' || requiresLogin === 1);
            const isReq = _isExt || _isReqVal;
            
            console.log('[BatRun-Auth] UI Update — isExternal:', _isExt, 'requiresLogin:', _isReqVal, '-> Final isReq:', isReq);

            const loginF = document.getElementById('loginFields');
            const noLoginF = document.getElementById('noLoginFields');
            const submitBtn = document.getElementById('submitBtn');
            const tabs = document.querySelector('.tabs');
            const t = publicTranslations[currentLang];

            if (isReq) {
                if (loginF) loginF.style.display = 'block';
                if (noLoginF) noLoginF.style.display = 'none';
                if (submitBtn) {
                    submitBtn.style.display = 'block';
                    submitBtn.innerText = isLogin ? (t.btnConnect || 'Connexion') : (t.btnRegister || 'Inscription');
                }
                if (tabs) tabs.style.display = 'flex';
            } else {
                if (loginF) loginF.style.display = 'none';
                if (noLoginF) noLoginF.style.display = 'block';
                if (submitBtn) submitBtn.style.display = 'none';
                if (tabs) tabs.style.display = 'none';
            }
        }

        loadNodes();
        setInterval(loadNodes, 5000);

        // EN: Show error if redirected after failed login / FR: Afficher erreur si redirection apres echec
        if (_loginError) {
            const msg = document.getElementById('message');
            if (msg) { msg.innerText = publicTranslations[currentLang]?.msgInvalid || 'Identifiants incorrects'; msg.className = 'msg-error'; }
        }

        // ==========================================
        // GAMEPAD NAVIGATION FOR CONNECT PAGE
        // ==========================================
        let _gpFocusedIndex = -1;
        let _gpLastButtons = {};
        const REPEAT_DELAY = 400;
        const REPEAT_MIN_INTERVAL = 50;
        const REPEAT_ACCEL = 0.8;
        let _gpRepeatTimers = {up:0, down:0, left:0, right:0};
        let _gpRepeatIntervals = {up:150, down:150, left:150, right:150};

        window.addEventListener('mousedown', () => { 
            _gpActive = false; 
            document.querySelectorAll('input').forEach(i => i.removeAttribute('inputmode'));
        });
        window.addEventListener('touchstart', () => { 
            _gpActive = false; 
            document.querySelectorAll('input').forEach(i => i.removeAttribute('inputmode'));
        });

        function getPubFocusables() {
            if (_activeKb && _activeKb.style.display === 'flex') {
                _gpContext = 'keyboard';
                return [..._activeKb.querySelectorAll('.vk-key')];
            }
            
            const res = [];
            document.querySelectorAll('.node-tile').forEach(n => res.push(n));
            const btnStart = document.getElementById('btnGuestStart');
            if (btnStart && btnStart.offsetParent !== null) res.push(btnStart);
            const user = document.getElementById('username');
            if (user && user.offsetParent !== null) res.push(user);
            const pass = document.getElementById('password');
            if (pass && pass.offsetParent !== null) res.push(pass);
            const submit = document.getElementById('submitBtn');
            if (submit && submit.offsetParent !== null) res.push(submit);
            const lnk = document.getElementById('lnkAdmin');
            if (lnk && lnk.offsetParent !== null) res.push(lnk);
            
            _gpContext = 'auth';
            return res;
        }

        function gpSetFocus(index) {
            const els = getPubFocusables();
            els.forEach(el => el.classList.remove('focused'));
            _gpFocusedIndex = Math.max(0, Math.min(index, els.length - 1));
            const el = els[_gpFocusedIndex];
            if (el) {
                el.classList.add('focused');
                el.scrollIntoView({ behavior: 'smooth', block: 'center' });
                if (el.tagName === 'INPUT') {
                    _activeInput = el;
                    // [BATRUN-MOD]: Prevent native keyboard on mobile when using gamepad
                    if (_gpActive) el.setAttribute('inputmode', 'none');
                }
            }
        }

        (function pollGamepads() {
            const gps = navigator.getGamepads ? navigator.getGamepads() : [];
            for (const gp of gps) {
                if (!gp) continue;
                const prev = _gpLastButtons[gp.index] || {};
                const pressed = (btn, i) => btn.pressed && !prev[i];

                const ax0 = gp.axes[0] || 0, ax1 = gp.axes[1] || 0;
                const now = performance.now();

                const checkRepeat = (dir, isPressed) => {
                    if (!isPressed) {
                        _gpRepeatTimers[dir] = 0;
                        _gpRepeatIntervals[dir] = 150;
                        return false;
                    }
                    if (_gpRepeatTimers[dir] === 0) {
                        _gpRepeatTimers[dir] = now + REPEAT_DELAY;
                        return true;
                    }
                    if (now > _gpRepeatTimers[dir]) {
                        _gpRepeatTimers[dir] = now + _gpRepeatIntervals[dir];
                        _gpRepeatIntervals[dir] = Math.max(REPEAT_MIN_INTERVAL, _gpRepeatIntervals[dir] * REPEAT_ACCEL);
                        return true;
                    }
                    return false;
                };

                const dRight = checkRepeat('right', gp.buttons[15]?.pressed || ax0 > 0.7);
                const dLeft  = checkRepeat('left', gp.buttons[14]?.pressed || ax0 < -0.7);
                const dDown  = checkRepeat('down', gp.buttons[13]?.pressed || ax1 > 0.7);
                const dUp    = checkRepeat('up', gp.buttons[12]?.pressed || ax1 < -0.7);

                const ctxEls = getPubFocusables();

                if (dRight || dLeft || dDown || dUp) {
                    _gpActive = true;
                    if (_gpContext === 'keyboard') {
                        const cols = 10;
                        let ni = _vkFocusedIndex;
                        if (dRight) ni++;
                        else if (dLeft) ni--;
                        else if (dDown) ni += cols;
                        else if (dUp) ni -= cols;
                        ni = Math.max(0, Math.min(ni, ctxEls.length - 1));
                        _vkFocusedIndex = ni;
                        ctxEls.forEach(k => k.classList.remove('focused'));
                        if(ctxEls[ni]) {
                            ctxEls[ni].classList.add('focused');
                            ctxEls[ni].scrollIntoView({block:'nearest'});
                        }
                    } else {
                        // Node list / Auth Navigation
                        let ni = _gpFocusedIndex < 0 ? 0 : _gpFocusedIndex;
                        if (dRight) ni++;
                        else if (dLeft) ni--;
                        else if (dDown) ni++;
                        else if (dUp) ni--;
                        
                        ni = Math.max(0, Math.min(ni, ctxEls.length - 1));
                        gpSetFocus(ni);
                    }
                }

                // A button (Confirm)
                if (pressed(gp.buttons[0], 0)) {
                    if (_gpContext === 'keyboard') {
                        if(ctxEls[_vkFocusedIndex]) ctxEls[_vkFocusedIndex].click();
                    } else {
                        if (_gpFocusedIndex >= 0 && ctxEls[_gpFocusedIndex]) ctxEls[_gpFocusedIndex].click();
                        else if (ctxEls.length) gpSetFocus(0);
                    }
                }
    
                // B button (Cancel/Back)
                if (pressed(gp.buttons[1], 1)) {
                    if (_gpContext === 'keyboard') {
                        closeVirtualKeyboard();
                    }
                }

                // X or Start (Open Keyboard)
                if (pressed(gp.buttons[2], 2) || pressed(gp.buttons[9], 9)) {
                    const _authPage = document.getElementById('authPage');
                    const _loginFields = document.getElementById('loginFields');
                    const _vkBlocked = _authPage && _authPage.style.display !== 'none' && (!_loginFields || _loginFields.style.display === 'none');

                    if (!_vkBlocked && _gpContext === 'auth') {
                        if (!_activeInput) {
                            const authInputs = document.querySelectorAll('#authContainer input');
                            if (authInputs.length > 0 && authInputs[0].offsetParent !== null) {
                                _activeInput = authInputs[0];
                            }
                        }
                        if (_activeInput) openVirtualKeyboard();
                    }
                }

                const newPrev = {};
                gp.buttons.forEach((btn, i) => newPrev[i] = btn.pressed);
                newPrev.ax0 = ax0; newPrev.ax1 = ax1;
                _gpLastButtons[gp.index] = newPrev;
            }
            requestAnimationFrame(pollGamepads);
        })();
        // ==========================================
    </script>
</body>
</html>";
            bool effectiveRequiresLogin = _manager.PublicAccessRequiresLogin || isExternal;
            return html
                .Replace("{GetStaticNodeHtml()}", GetStaticNodeHtml(isExternal))
                .Replace("{AUTH_LOGIN_DISPLAY}", effectiveRequiresLogin ? "block" : "none")
                .Replace("{AUTH_NO_LOGIN_DISPLAY}", effectiveRequiresLogin ? "none" : "block")
                .Replace("{AUTH_TABS_DISPLAY}", effectiveRequiresLogin ? "flex" : "none")
                .Replace("{AUTH_REGISTER_DISPLAY}", _manager.PublicAccessAllowRegistration ? "block" : "none")
                .Replace("{AUTH_ADMIN_LINK_BLOCK}", canAccessAdmin
                    ? @"<div style=""margin-top: 25px; text-align: center; border-top: 1px solid rgba(255,255,255,0.05); padding-top: 20px;"">"
                      + @"<a href=""/admin"" id=""lnkAdmin"" style=""color: var(--primary); text-decoration: none; font-size: 0.85rem; font-weight: bold; display: flex; align-items: center; justify-content: center; gap: 8px; transition: 0.3s;"">"
                      + @"<span style=""color: var(--primary); font-size: 1rem;"">⚙️</span> Accès Administrateur / Opérateur</a></div>"
                    : "")
                .Replace("{AUTH_SUBMIT_DISPLAY}", effectiveRequiresLogin ? "block" : "none")
                .Replace("{AUTH_GUEST_START_LABEL}", _manager.Config.ReadValue("Arcade", "Language", "fr") == "fr" ? "DÉMARRER" : "START")
                .Replace("{REQUIRES_LOGIN_JS}", effectiveRequiresLogin.ToString().ToLower())
                .Replace("{IS_EXTERNAL_JS}", isExternal.ToString().ToLower());
        }
    }
}
