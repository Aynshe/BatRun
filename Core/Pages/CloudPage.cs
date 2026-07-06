// EN: Partial class for ArcadeApiService - Cloud page HTML generation (games/streaming)
// FR: Classe partielle pour ArcadeApiService - Generation HTML de la page Cloud (jeux/streaming)
// EN: This file contains the GetCloudPageHtml() method - NEVER served without server-side token validation
// FR: Ce fichier contient la methode GetCloudPageHtml() - JAMAIS servi sans validation token cote serveur

using System;
using System.Linq;

using BatRun;
using BatRun.Core;

namespace BatRun.Core
{
    public partial class ArcadeApiService
    {
        // EN: GetStaticNodeHtml() is in ConnectPage.cs (not needed on cloud page)
        // FR: GetStaticNodeHtml() est dans ConnectPage.cs (non necessaire sur la page cloud)


        private string GetCloudPageHtml(bool canAccessAdmin, bool isExternal)
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
        #systemSelectModal, #genreSelectModal, #sortSelectModal {
            position: absolute; top: 0; left: 0; width: 100%; height: 100%;
            background: rgba(15, 23, 42, 0.95); backdrop-filter: blur(10px);
            z-index: 10000; display: none; flex-direction: column; align-items: center; padding-top: 50px;
        }
        .system-grid {
            display: grid; grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
            justify-content: center;
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

        
        .filter-btn {
            flex: 1 1 140px;
            background: rgba(0,0,0,0.4) !important;
            border: 1px solid var(--card-border) !important;
            border-radius: 10px !important;
            color: white !important;
            padding: 0 12px !important;
            font-size: 0.85rem !important;
            height: 45px !important;
            max-height: 45px !important;
            min-height: 45px !important;
            text-align: center !important;
            white-space: nowrap !important;
            overflow: hidden !important;
            text-overflow: ellipsis !important;
            box-shadow: none !important;
            margin: 0 !important;
        }
        
        .filter-btn.filter-active {
            background: linear-gradient(135deg, var(--primary), var(--secondary)) !important;
            color: #000 !important;
            border-color: transparent !important;
            box-shadow: 0 0 15px rgba(0,210,255,0.6) !important;
        }
        .filter-btn.focused, .filter-btn:hover {
            background: linear-gradient(135deg, var(--primary), var(--secondary)) !important;
            color: #000 !important;
            border-color: transparent !important;
            transform: scale(1.03) !important;
            box-shadow: 0 5px 15px rgba(0,210,255,0.4) !important;
            z-index: 10;
        }

        /* Focus state for gamepad navigation */
        .game-item.focused, #customSystemSelect.focused, #customGenreSelect.focused, #customSortSelect.focused, #btnHistoryToggle.focused, #gameSearchInput.focused, #btnOpenRbModal.focused, #lobbyJoinBannerBtn.focused {
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
            .filter-btn { height: 38px !important; font-size: 0.7rem !important; padding: 0 5px !important; flex: 1 1 30% !important; }
            #pullHandle { display: block; }
        }

        /* Portrait adaptations specifically for search bar */
        @media (max-width: 500px) and (orientation: portrait) {
            #searchInnerRow { flex-direction: row !important; flex-wrap: wrap !important; gap: 5px !important; }
            #gameSearchInput { width: 100% !important; flex: 1 1 100% !important; order: -1; }
            .filter-btn { flex: 1 1 45% !important; }
        }

        /* Landscape adaptations for smaller screens */
        @media (max-height: 500px) and (orientation: landscape) {
            .game-grid { grid-template-columns: repeat(auto-fill, minmax(130px, 1fr)) !important; gap: 8px !important; }
            .modal-content { max-height: 75vh !important; padding: 15px !important; width: 95% !important; max-width: 800px !important; }
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
        #mdVideo.focused { outline: 3px solid var(--primary); transform: scale(1.02); }
    </style>
</head>
<body>
    <div class=""bg-glow""></div>

    <div id=""gameContainer"" style=""display:flex;"">
        <div id=""pullHandle"" onclick=""showControls()""></div>
        <div class=""header"" id=""publicHeader"">
            <h1 style=""margin: 0; font-size: 1.5em;"" id=""viewTitle"">🕹️ BatRun Connect</h1>
            <div class=""header-actions"">
                <div class=""lang-switch"">
                    <span class=""flag"" id=""flag-fr"" onclick=""setLanguage('fr')""><img src=""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 3 2'%3E%3Crect width='3' height='2' fill='%23ED2939'/%3E%3Crect width='2' height='2' fill='%23fff'/%3E%3Crect width='1' height='2' fill='%23002395'/%3E%3C/svg%3E"" style=""width:16px; height:11px; border-radius:1px;"" alt=""FR""> FR</span>
                    <span class=""flag"" id=""flag-en"" onclick=""setLanguage('en')""><img src=""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 60 30'%3E%3CclipPath id='t'%3E%3Cpath d='M30,15h30v15zv15H0zH0V0zV0h30z'/%3E%3C/clipPath%3E%3Cpath d='M0,0v30h60V0z' fill='%23012169'/%3E%3Cpath d='M0,0 60,30M60,0 0,30' stroke='%23fff' stroke-width='6'/%3E%3Cpath d='M0,0 60,30M60,0 0,30' clip-path='url(%23t)' stroke='%23C8102E' stroke-width='4'/%3E%3Cpath d='M30,0v30M0,15h60' stroke='%23fff' stroke-width='10'/%3E%3Cpath d='M30,0v30M0,15h60' stroke='%23C8102E' stroke-width='6'/%3E%3C/svg%3E"" style=""width:16px; height:11px; border-radius:1px;"" alt=""EN""> EN</span>
                </div>
                <button id=""btnStopSessionLobby"" onclick=""cancelCurrentSession()"" style=""display:none; width:auto; padding:8px 15px; background:rgba(255,165,0,0.1); border:1px solid rgba(255,165,0,0.3); color:#ffa500; margin-right:5px;""></button>
                <button id=""btnFullscreenPage"" onclick=""togglePageFullscreen()"" style=""width: auto; padding: 8px 15px; background: rgba(0, 242, 255, 0.1); border: 1px solid rgba(0, 242, 255, 0.3); color: var(--primary); margin-right: 5px;"" title=""Plein Écran"">⛶</button>
                <button id=""logoutBtn"" onclick=""logout()"">Sortir</button>
            </div>
        </div>
        
        <!-- Search and Filter -->
        <div id=""publicSearchContainer"" style=""display: flex; flex-direction: column; width: 100%; box-sizing: border-box; margin-top: 15px; margin-bottom: 5px;"">
            <div id=""searchInnerRow"" style=""display: flex; flex-direction: row; flex-wrap: wrap; gap: 8px; width: 100%; align-items: center;"">
                <button id=""customSystemSelect"" class=""filter-btn"" onclick=""openSystemModal()"">TOUS LES SYSTÈMES</button>
                <button id=""customGenreSelect"" class=""filter-btn"" onclick=""openGenreModal()"">TOUS LES GENRES</button>
                <button id=""customSortSelect"" class=""filter-btn"" onclick=""openSortModal()"">Tri : Nom (A-Z)</button>
                <input type=""text"" id=""gameSearchInput"" class=""filter-btn"" placeholder=""Rechercher un jeu..."" oninput=""searchGamesDebounced()"" style=""flex: 2 1 200px; text-align: left !important;"">
                <button id=""btnOpenRbModal"" class=""filter-btn"" onclick=""openRbConfirm()"">RETROBAT</button>
                <button id=""btnHistoryToggle"" class=""filter-btn"" onclick=""toggleHistoryFilter()"">HISTORIQUE</button>
            </div>
            <!-- [BATRUN-FORK-v6] Lobby/Session join banner — shown when a lobby or game session is active -->
            <!-- FR: Bannière pour rejoindre un lobby/session — affichée quand un lobby ou une session est actif -->
            <div id=""lobbyJoinBanner"" style=""display:none; margin-top:8px; padding:10px 20px; border-radius:15px; background:linear-gradient(135deg, rgba(112,0,255,0.15), rgba(0,242,255,0.1)); border:1px solid rgba(0,242,255,0.3); color:white; align-items:center; justify-content:center; gap:20px; box-shadow: 0 8px 32px rgba(0,0,0,0.4); backdrop-filter:blur(12px); -webkit-backdrop-filter:blur(12px); width:100%; box-sizing:border-box; overflow:hidden;"">
                <div style=""display:flex; align-items:center; gap:12px; flex-shrink:0; min-width:0;"">
                    <span style=""font-size:1.5rem; filter: drop-shadow(0 0 5px var(--primary)); flex-shrink:0;"">🎮</span>
                    <span id=""lobbyJoinBannerText"" style=""font-weight: 600; font-size:1.05rem; letter-spacing: 0.5px; text-shadow: 0 2px 4px rgba(0,0,0,0.5); white-space:nowrap; overflow:hidden; text-overflow:ellipsis;"">Lobby en cours</span>
                </div>
                <button id=""lobbyJoinBannerBtn"" onclick=""joinExistingSession()"" style=""width: auto !important; flex: 0 0 auto; min-width:120px; padding:10px 20px; border:none; border-radius:10px; background:linear-gradient(135deg, var(--primary), #3a7bd5); color:#000; font-weight:bold; cursor:pointer; font-size:0.95rem; transition: all 0.3s cubic-bezier(0.175, 0.885, 0.32, 1.275); box-shadow: 0 4px 15px rgba(0,0,0,0.3);"" onmouseover=""this.style.transform='scale(1.05) translateY(-2px)'; this.style.box-shadow='0 8px 25px rgba(0,210,255,0.4)';"">Rejoindre</button>
            </div>
            <!-- Virtual Keyboard Search: centered relative to search bar total width -->
            <div id=""searchVirtualKeyboard"" class=""virtual-keyboard"" style=""max-width: 850px; margin: 12px auto;""></div>
        </div>

        <!-- Game List View -->
        <div id=""listView"">
            <div id=""publicGamesList"" class=""game-grid"">
                <p id=""txtLoadingGames"">Initialisation des jeux...</p>
            </div>
        </div>

        <!-- Stream View -->
        <div id=""streamView"">
            <div id=""gameLaunchOverlay"" style=""display:none; position:absolute; top:0; left:0; width:100%; height:100%; background:radial-gradient(ellipse at 30% 20%, rgba(112,0,255,0.2), transparent 50%), radial-gradient(ellipse at 70% 80%, rgba(0,242,255,0.2), transparent 50%), linear-gradient(135deg, #0a0a1a 0%, #0f172a 50%, #0a0a1a 100%); z-index:9999; flex-direction:column; justify-content:center; align-items:center; color:white; font-family:'Outfit',sans-serif; text-align:center;"">
                <h2 id=""overlayText"" style=""margin-bottom:10px;"">Connexion au flux vidéo...</h2>
                <div class=""loader-spinner"" id=""overlayLoader""></div>
            </div>
            <!-- [BATRUN-FORK] Multiplayer Lobby Overlay -->
            <div id=""lobbyOverlay"" class=""lobby-overlay"">
                <div id=""lobbyWaitingArea"" style=""display:flex; flex-direction:column; align-items:center;"">
                    <div class=""lobby-waiting-text"" id=""lobbyWaitingText"">En attente de joueurs...</div>
                    <div class=""loader-spinner""></div>
                    <div class=""lobby-choices"">
                        <div class=""lobby-choice lobby-choice-yes"" id=""lobbyReadyBtn"" style=""cursor:pointer;"" onclick=""document.getElementById('btnLobbyReadyInternal').click()"">START = PRÊT<button id=""btnLobbyReadyInternal"" onclick=""fetch(getRelayUrl('/api/public/lobby/ready'), {method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify({ token: localStorage.getItem('batrun_token'), sessionId: _lobbySessionId })}).then(() => lobbyPollStatus());"" style=""display:none;""></button></div>
                        <div class=""lobby-choice lobby-choice-no"" id=""lobbySoloBtn"" style=""cursor:pointer;"" onclick=""lobbySoloLaunch()"">X = CONTINUER SEUL</div>
                        <div class=""lobby-choice lobby-choice-cancel"" id=""lobbyCancelBtn"" style=""cursor:pointer; background: rgba(255,50,50,0.2); border-color: rgba(255,50,50,0.5);"" onclick=""cancelCurrentSession()"">B = QUITTER LOBBY</div>
                    </div>
                </div>
                <div id=""lobbyConfirmArea"" style=""display:none; flex-direction:column; align-items:center;"">
                    <div class=""lobby-question"" id=""lobbyConfirmText"">Tous prêts ! START pour lancer</div>
                </div>
            </div>
            <!-- [BATRUN-FORK] Player Banner (bottom bar showing connected players) -->
            <div id=""lobbyBanner"" class=""lobby-banner"">
                <div class=""lobby-banner-label"">🎮 JOUEURS</div>
                <div id=""lobbyPlayerSlots"" style=""display:flex; gap:10px; flex-wrap:wrap;""></div>
            </div>
            <div class=""stream-control-top-left"">
                <button onclick=""requestStreamFullscreen()"" id=""btnFullStream"">Plein Écran</button>
            </div>
            <div class=""stream-overlay""><div class=""live-dot""></div> <span id=""txtLiveStream"">LIVE STREAM</span></div>
            <iframe id=""streamIframe"" 
                    src=""about:blank"" 
                    allow=""fullscreen; gamepad; pointer-lock; autoplay"">
            </iframe>
            
            <div id=""escOverlay"" class=""esc-overlay"">
                <div class=""esc-ring"">
                    <svg viewBox=""0 0 150 150"">
                        <circle class=""esc-circle-bg"" cx=""75"" cy=""75"" r=""70""></circle>
                        <circle id=""escCircle"" class=""esc-circle-fg"" cx=""75"" cy=""75"" r=""70""></circle>
                    </svg>
                    <div class=""esc-text"" id=""escSec"">3s</div>
                </div>
                <div class=""esc-msg"" id=""escMsg"">Maintenez ÉCHAP pour quitter</div>
            </div>

            <!-- [BATRUN] Emergency Stop Overlay — Select+Start 5s long press progress ring -->
            <!-- FR: Overlay d'arrêt d'urgence — anneau de progression pression longue Select+Start 5s -->
            <div id=""emergencyStopOverlay"" class=""esc-overlay"" style=""z-index:100000;"">
                <div class=""esc-ring"">
                    <svg viewBox=""0 0 150 150"">
                        <circle class=""esc-circle-bg"" cx=""75"" cy=""75"" r=""70""></circle>
                        <circle id=""emergencyCircle"" class=""esc-circle-fg"" cx=""75"" cy=""75"" r=""70"" style=""stroke: #ff3c5c; filter: drop-shadow(0 0 8px #ff3c5c);""></circle>
                    </svg>
                    <div class=""esc-text"" id=""emergencySec"" style=""color: #ff3c5c;"">5s</div>
                </div>
                <div class=""esc-msg"" id=""emergencyMsg"" style=""color: #ff3c5c;"">SELECT + START = FERMER LA SESSION</div>
            </div>

            <!-- [BATRUN] Emergency Stop Confirmation Modal -->
            <!-- FR: Modal de confirmation de fermeture de session -->
            <div id=""emergencyConfirmModal"" style=""position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.9); backdrop-filter: blur(15px); display: none; align-items: center; justify-content: center; z-index: 100001; opacity: 0; transition: opacity 0.3s;"">
            <div style=""background: rgba(20,20,30,0.95); border: 2px solid #ff3c5c; border-radius: 20px; max-width: 500px; width: 90%; padding: 40px; text-align: center; box-shadow: 0 0 40px rgba(255,60,92,0.3);"">
            <div style=""font-size: 3rem; margin-bottom: 15px;"">🛑</div>
            <h2 id=""emergencyConfirmTitle"" style=""color: #ff3c5c; margin-bottom: 15px; font-size: 1.5rem;"">FERMETURE DE SESSION</h2>
            <p id=""emergencyConfirmDesc"" style=""color: #ccc; margin-bottom: 30px; font-size: 1rem; line-height: 1.6;"">Voulez-vous forcer la fermeture de la session ? Tous les joueurs seront déconnectés.</p>
                    <div style=""display: flex; gap: 20px; justify-content: center;"">
                        <button id=""btnEmergencyCancel"" onclick=""cancelEmergencyStop()"" style=""background: #444; color: white; padding: 12px 30px; border-radius: 12px; cursor: pointer; font-weight: bold; border: none; font-size: 1rem;"">Annuler</button>
                        <button id=""btnEmergencyConfirm"" onclick=""confirmEmergencyStop()"" style=""background: #ff3c5c; color: white; padding: 12px 30px; border-radius: 12px; cursor: pointer; font-weight: bold; border: none; font-size: 1rem; box-shadow: 0 0 15px rgba(255,60,92,0.4);"">CONFIRMER</button>
                    </div>
                </div>
            </div>

            <button onclick=""stopLaunch()"" id=""btnStopGame"" style=""position: absolute; bottom: 20px; left: 50%; transform: translateX(-50%); width: auto; background: rgba(0,0,0,0.7); color: white; border: 1px solid white;"">Quitter le jeu</button>
        </div>
    </div>

    <!-- System Select Modal -->
    <div id=""systemSelectModal"">
        <h2 id=""sysModalTitle"" style=""color: white; margin-bottom: 20px;"">SÉLECTIONNER UN SYSTÈME</h2>
        <div class=""system-grid"" id=""systemSelectGrid""></div>
        <button class=""modal-close"" style=""position: absolute; top: 20px; right: 20px; background: transparent; color: white; border: none; font-size: 2rem; cursor: pointer;"" onclick=""closeSystemModal()"">✖</button>
    </div>

    <!-- Genre Select Modal -->
    <div id=""genreSelectModal"">
        <h2 id=""genreModalTitle"" style=""color: white; margin-bottom: 20px;"">SÉLECTIONNER UN GENRE</h2>
        <div class=""system-grid"" id=""genreSelectGrid""></div>
        <button class=""modal-close"" style=""position: absolute; top: 20px; right: 20px; background: transparent; color: white; border: none; font-size: 2rem; cursor: pointer;"" onclick=""closeGenreModal()"">✖</button>
    </div>

    <!-- Sort Select Modal -->
    <div id=""sortSelectModal"">
        <h2 id=""sortModalTitle"" style=""color: white; margin-bottom: 20px;"">TRIER PAR</h2>
        <div class=""system-grid"" id=""sortSelectGrid""></div>
        <button class=""modal-close"" style=""position: absolute; top: 20px; right: 20px; background: transparent; color: white; border: none; font-size: 2rem; cursor: pointer;"" onclick=""closeSortModal()"">✖</button>
    </div>

    <!-- RetroBat Confirmation Modal -->
    <div id=""rbConfirmModal"" style=""position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.85); backdrop-filter: blur(10px); display: none; align-items: center; justify-content: center; z-index: 2500; opacity: 0; transition: opacity 0.3s;"">
        <div class=""modal-content"" style=""text-align: center; max-width: 450px;"">
            <h2 id=""rbModalTitle"" style=""color: white; margin-bottom: 20px;"">Lancer RetroBat ?</h2>
            <p id=""rbModalDesc"" style=""color: #bbb; margin-bottom: 30px;"">Vous êtes sur le point de lancer l'interface complète de RetroBat. Confirmer ?</p>
            <div style=""display: flex; gap: 20px; justify-content: center;"">
                <button id=""btnRbCancel"" onclick=""closeRbConfirm()"" style=""background: #555; color: white; padding: 10px 20px; border-radius: 5px; cursor: pointer; font-weight: bold; border: none; width: 120px;"">Annuler</button>
                <button id=""btnRbConfirm"" onclick=""executeRbLaunch()"" style=""background: var(--success); color: white; padding: 10px 20px; border-radius: 5px; cursor: pointer; font-weight: bold; border: none; width: 120px;"">Lancer</button>
            </div>
        </div>
    </div>

    <!-- [BATRUN-v3]: Stream Retry Modal -->
    <div id=""retryConfirmModal"" style=""position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.85); backdrop-filter: blur(10px); display: none; align-items: center; justify-content: center; z-index: 2600; opacity: 0; transition: opacity 0.3s;"">
        <div class=""modal-content"" style=""text-align: center; max-width: 450px;"">
            <h2 id=""retryModalTitle"" style=""color: white; margin-bottom: 20px;"">Échec de connexion</h2>
            <p id=""retryModalDesc"" style=""color: #bbb; margin-bottom: 30px;"">La connexion au flux vidéo a échoué. Voulez-vous réessayer ?</p>
            <div style=""display: flex; gap: 20px; justify-content: center;"">
                <button id=""btnRetryCancel"" onclick=""closeRetryModal()"" style=""background: #555; color: white; padding: 10px 20px; border-radius: 5px; cursor: pointer; font-weight: bold; border: none; width: 120px;"">Non</button>
                <button id=""btnRetryConfirm"" onclick=""executeRetryLaunch()"" style=""background: var(--success); color: white; padding: 10px 20px; border-radius: 5px; cursor: pointer; font-weight: bold; border: none; width: 120px;"">Oui</button>
            </div>
        </div>
    </div>

    <!-- Game Details Modal -->
    <div id=""gameModal"" onclick=""if(event.target === this) closeGameDetails()"">
        <div class=""modal-content"">
            <button class=""modal-close"" onclick=""closeGameDetails()"">✖</button>
            <div class=""modal-media"">
                <img id=""mdImage"" src="""" style=""width:100%; border-radius:8px; display:none; max-height:40vh; object-fit:contain;"">
                <video id=""mdVideo"" src="""" autoplay loop muted onmouseenter=""this.muted=false"" onmouseleave=""this.muted=true"" style=""width:100%; border-radius:8px; display:none; max-height:40vh; background:#000; transition: transform 0.2s, outline 0.2s;""></video>
            </div>
            <div style=""font-size: 0.8rem; color: var(--primary); font-weight: bold; text-transform: uppercase; margin-bottom: 5px;"" id=""mdSystem"">SYSTEM</div>
            <h2 style=""margin: 0 0 15px 0; font-size: 1.5rem;"" id=""mdTitle"">GAME TITLE</h2>
            <div style=""font-size: 0.9rem; opacity: 0.8; line-height: 1.5; margin-bottom: 20px; max-height: 15vh; overflow-y: auto;"" id=""mdDesc"">...</div>
            <button id=""btnLaunchModal"" class=""play-btn"" style=""width: 250px; margin: 0 auto; display: block; font-size: 1.1rem; padding: 15px; background: linear-gradient(135deg, var(--primary), var(--secondary)); color: #000; border:none;"">DÉMARRER</button>
        </div>
    </div>

    <script>
        let isLogin = true;
        let isStreaming = false;
        let requiresLogin = {REQUIRES_LOGIN_JS};
        let isExternalClient = {IS_EXTERNAL_JS};
        let apiBase = ''; // Dynamic based on selected machine
        const localApiBase = '';
        const moonlightPort = 8080;
        let selectedNodeObj = null; // [BATRUN-IRON]: Track selected node object for redirection
        let selectedNodeName = null; // [BATRUN-FIX]: Track selected node name to maintain highlight after refresh

        // [BATRUN-IRON]: Handle token passed via URL (for post-login redirection)
        const urlParams = new URLSearchParams(window.location.search);
        const urlToken = urlParams.get('token');
        if (urlToken) {
            localStorage.setItem('batrun_token', urlToken);
            console.log('[BatRun] Token received via URL redirection.');
            // FR: Nettoyer l'URL pour la propreté / EN: Clean URL
            window.history.replaceState({}, document.title, window.location.pathname);
        }

        // [BATRUN-CRED] EN: Show error message if redirected back after failed form-login
        // FR: Afficher une erreur si redirigé après un échec de connexion via formulaire
        const _loginError = urlParams.get('login_error');

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
                genreFilterAll: ""TOUS LES GENRES"",
                genreModalTitle: ""SÉLECTIONNER UN GENRE"",
                sortModalTitle: ""TRIER PAR"",
                sortAlpha: ""Tri : Nom (A-Z)"",
                sortSystem: ""Tri : Système (A-Z)"",
                historyBtn: ""HISTORIQUE"",
                historyBtnActive: ""HISTORIQUE (ACTIF)"",
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
                btnStartGameServer: ""DÉMARRER LE SERVEUR DE JEUX"",
                localBusy: ""Occupé (Jeu local)"",
                localBusyHint: ""Un jeu est en cours sur la machine physiquement."",
                retryModalTitle: ""Échec de connexion"",
                retryModalDesc: ""La connexion au flux vidéo a échoué. Voulez-vous réessayer ?"",
                retryCancel: ""Non"",
                retryConfirm: ""Réessayer""
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
                genreFilterAll: ""ALL GENRES"",
                genreModalTitle: ""SELECT A GENRE"",
                sortModalTitle: ""SORT BY"",
                sortAlpha: ""Sort: Name (A-Z)"",
                sortSystem: ""Sort: System (A-Z)"",
                historyBtn: ""HISTORY"",
                historyBtnActive: ""HISTORY (ACTIVE)"",
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
                btnStartGameServer: ""START GAME SERVER"",
                localBusy: ""Busy (Local Game)"",
                localBusyHint: ""A game is currently running on the physical machine."",
                retryModalTitle: ""Connection Failed"",
                retryModalDesc: ""Connection to video stream failed. Would you like to retry?"",
                retryCancel: ""No"",
                retryConfirm: ""Retry""
                }
        };

        // EN: Restore language from localStorage, fallback to browser language
        // FR: Restaurer la langue depuis localStorage, sinon langue du navigateur
        let currentLang = localStorage.getItem('batrun_lang') || (navigator.language.startsWith('fr') ? 'fr' : 'en');
        const isMobile = /iPhone|iPad|iPod|Android/i.test(navigator.userAgent);

        let searchTimer = null;
        let currentGamesList = [];
        let renderPage = 0;
        const RENDER_CHUNK = 50;
        let scrollObserver = null;
        let systemsLoaded = false;
        let globalStatus = null; // store last status for playing checks

        // [BATRUN-FORK]: Launch Synchronization
        let pendingLaunchPath = null;
        let isWaitingForML = false;
        let waitStartTimestamp = 0;
        let isWaitingForInput = false;
        let localLastGameEndTime = 0;
        let localForceStopTime = 0; // EN: Track last force-stop timestamp from server / FR: Suivre le dernier timestamp d'arrêt forcé du serveur
        let isRbStream = false;
        let _sessionGameStarted = false; // [BATRUN-v3]: Track if the game successfully started at least once during this stream session
        let _lastReconnectAttempt = 0; // EN: Timestamp of last auto-reconnect attempt / FR: Timestamp de la dernière tentative de reconnexion auto
        const RECONNECT_COOLDOWN_MS = 5000; // EN: 5s cooldown between reconnect attempts / FR: 5s de cooldown entre les tentatives de reconnexion
        // [BATRUN-FORK-v8]: Cooldown timestamp after stopping a stream.
        // When a stream is stopped, the virtual controller on the host takes a few seconds
        // to disconnect. We must not start a new stream until the old controller is gone.
        // FR: Timestamp de cooldown après l'arrêt d'un stream.
        // Quand un stream est arrêté, la manette virtuelle sur l'hôte met quelques secondes
        // à se déconnecter. On ne doit pas démarrer un nouveau stream avant que l'ancienne
        // manette soit supprimée.
        let _batrunStreamCooldownUntil = 0;
        
        // [BATRUN-FORK-v8]: Start a Moonlight stream in the iframe, respecting the cooldown.
        // If a previous stream was recently stopped, this waits for the remaining cooldown
        // before setting iframe.src, so the old virtual controller has time to disconnect.
        // FR: Démarrer un stream Moonlight dans l'iframe, en respectant le cooldown.
        // Si un stream précédent a été arrêté récemment, attend le cooldown restant
        // avant de définir iframe.src, pour que l'ancienne manette virtuelle ait le temps
        // de se déconnecter.
        function batrunStartStream(iframe, streamUrl, onFocus) {
        const now = Date.now();
        if (now < _batrunStreamCooldownUntil) {
        const waitMs = _batrunStreamCooldownUntil - now;
        console.log('[BatRun] Stream cooldown active, waiting ' + waitMs + 'ms for virtual controller cleanup...');
        setTimeout(() => {
        iframe.src = streamUrl;
        if (onFocus) onFocus();
        }, waitMs);
        } else {
        iframe.src = streamUrl;
        if (onFocus) onFocus();
        }
        }
        
        // [BATRUN-FORK]: Multiplayer Lobby State (server-coordinated)
        // FR: État du lobby multijoueur (coordonné par le serveur)
        // The server holds the authoritative lobby state; clients poll /lobby/status
        let lobbyPhase = 'none'; // local mirror of server phase
        let _lobbySessionId = 'sess_' + Math.random().toString(36).substr(2, 9) + '_' + Date.now();
        let _lobbyPollTimer = null;
        let _lobbyActionCooldown = 0; // EN: Cooldown between lobby gamepad actions / FR: Cooldown entre les actions manette du lobby
        
        // [BATRUN-FORK] Navigation State Memory & Repeat logic
        let _savedGpIndex = 0;
        let _gpRepeatTimers = { up: 0, down: 0, left: 0, right: 0 };
        let _gpRepeatIntervals = { up: 150, down: 150, left: 150, right: 150 };
        const REPEAT_DELAY = 400; // ms before start repeating
        const REPEAT_MIN_INTERVAL = 40; // ms (fastest speed)
        const REPEAT_ACCEL = 0.85; // interval = interval * 0.85 each step
        
        // [BATRUN-FORK] ESC Long Press state
        let _escHoldStart = 0;
        let _escHoldInterval = null;
        const ESC_HOLD_DURATION = 3000;
        const ESC_OVERLAY_DELAY = 1000;

        // [BATRUN] Emergency Stop — Select+Start Long Press state (5 seconds)
        // FR: Arrêt d'urgence — état de la pression longue Select+Start (5 secondes)
        let _emergencyHoldStart = 0;
        let _emergencyHoldInterval = null;
        const EMERGENCY_HOLD_DURATION = 5000;
        const EMERGENCY_OVERLAY_DELAY = 800; // EN: Show overlay after 800ms of holding / FR: Afficher l'overlay après 800ms de maintien
        let _emergencyComboActive = false; // EN: True while both Select+Start are held / FR: Vrai pendant que Select+Start sont maintenus
        let _emergencyConfirmShown = false; // EN: True when confirmation modal is visible / FR: Vrai quand la modal de confirmation est visible

        window.addEventListener('message', (event) => {
            console.log('[BatRun-Dashboard] Message received:', event.data);
            if (event.data && event.data.type === 'ML_CONNECTED') {
                console.log('[ML-SYNC] Moonlight connected! Switching to input-wait state.');
                if (isWaitingForML && pendingLaunchPath) {
                    const t = publicTranslations[currentLang];
                    const overlayText = document.getElementById('overlayText');
                    if (overlayText) {
                        overlayText.innerHTML = t.msgPressAnyButton || '🎮 Appuyez sur un bouton de la manette pour démarrer 🎮<br><br><span style=""font-size:0.5em; opacity:0.5;"">(Ou Clic/Entrée)</span>';
                    }
                    isWaitingForInput = true;
                    // [BATRUN-FORK-v10]: Immediately block input in Moonlight iframe as soon as connected,
                    // to prevent the user's first interaction (dismissing the overlay) from leaking to host.
                    // FR: Bloquer immédiatement les inputs dans l'iframe Moonlight dès la connexion,
                    // pour éviter que l'interaction utilisateur (clic overlay) ne passe vers l'hôte.
                    const _blockIframe = document.getElementById('streamIframe');
                    if (_blockIframe && _blockIframe.contentWindow) {
                        try { _blockIframe.contentWindow.postMessage({type: 'BATRUN_LOBBY_INPUT_BLOCK', blocked: true}, '*'); } catch(e) {}
                    }
                }
            }
            // [BATRUN-v3]: Receive force-stop signal from Moonlight iframe (Select+Start 5s hold detected inside iframe)
            // FR: Recevoir le signal d'arrêt forcé depuis l'iframe Moonlight (pression longue Select+Start détectée dans l'iframe)
            if (event.data && event.data.type === 'BATRUN_FORCE_STOP') {
                console.log('[BatRun] BATRUN_FORCE_STOP received from Moonlight iframe! Triggering force-stop confirmation...');
                // EN: Show the emergency confirmation modal just like the parent page detection does
                // FR: Afficher la modal de confirmation de fermeture comme le fait la détection de la page parente
                if (!_emergencyConfirmShown && (isStreaming || isRbStream || isWaitingForML)) {
                    emergencyShowConfirm();
                }
            }
        });

        function triggerLaunch() {
            if (!isWaitingForInput) return;
            isWaitingForInput = false;
            
            // EN: Request fullscreen on the parent container so Moonlight can expand to the whole screen.
            // FR: Demander le plein écran sur le conteneur parent pour que Moonlight remplisse l'écran.
            try {
                const sv = document.getElementById('streamView');
                if (sv && sv.requestFullscreen) {
                    sv.requestFullscreen().catch(e => console.warn('FS error', e));
                }
            } catch(e) {}

            isWaitingForML = true;
            _sessionGameStarted = false; // Reset for new session
            waitStartTimestamp = Date.now();

            // EN: Pre-unlock audio in the parent frame (user gesture context).
            // FR: Pré-débloquer l'audio dans la frame parente (contexte de geste utilisateur).
            try {
                const _unlockCtx = new AudioContext();
                _unlockCtx.resume().then(() => _unlockCtx.close());
            } catch(e) {}

            // EN: Unlock Moonlight's AudioContext immediately during this user gesture
            // FR: Débloquer l'AudioContext Moonlight pendant ce geste utilisateur
            const iframe = document.getElementById('streamIframe');
            if (iframe) {
                try {
                    if (iframe.contentWindow && iframe.contentWindow.app) {
                        iframe.contentWindow.app.onUserInteraction();
                    }
                } catch(e) {}
            }

            // [BATRUN-FORK]: Check if lobby exists, then create/join accordingly
            // FR: Vérifier si le lobby existe, puis créer/rejoindre en conséquence
            const token = localStorage.getItem('batrun_token') || '';
            lobbyPhase = 'lobby';
            _lobbyActionCooldown = Date.now() + 500; // EN: 500ms cooldown to prevent START bleed from triggerLaunch

            // EN: Block gamepad input in Moonlight iframe to prevent ES from reacting
            // FR: Bloquer les inputs manette dans l'iframe Moonlight pour empêcher ES de réagir
            if (iframe && iframe.contentWindow) {
                try { iframe.contentWindow.postMessage({type: 'BATRUN_LOBBY_INPUT_BLOCK', blocked: true}, '*'); } catch(e) {}
                try { iframe.blur(); } catch(e) {}
            }

            // [BATRUN-FORK-v11]: Handle joining an existing session (P2+)
            // FR: Gérer la jonction à une session existante (P2+)
            if (pendingLaunchPath === '[JOIN]') {
                console.log('[Lobby] Joining existing session. Checking for active lobby...');
                const launchOverlay = document.getElementById('gameLaunchOverlay');
                const lobbyOverlay = document.getElementById('lobbyOverlay');
                
                const proceedToStream = (shouldBlockInput) => {
                    if (launchOverlay) launchOverlay.style.display = 'none';
                    if (iframe && iframe.contentWindow) {
                        // [BATRUN-FIX]: Correctly block input for P2 if a lobby is active
                        // FR: Bloquer correctement l'input pour P2 si un lobby est actif
                        try { iframe.contentWindow.postMessage({type: 'BATRUN_LOBBY_INPUT_BLOCK', blocked: shouldBlockInput}, '*'); } catch(e) {}
                        iframe.focus();
                    }
                    pendingLaunchPath = null;
                    isWaitingForML = false;
                    _sessionGameStarted = true;
                };

                if (globalStatus && (globalStatus.isLobbyActive || globalStatus.isLobbyWaiting)) {
                    console.log('[Lobby] Active lobby detected, calling /join');
                    fetch(getRelayUrl('/api/public/lobby/join'), {
                        method: 'POST',
                        headers: {'Content-Type': 'application/json'},
                        body: JSON.stringify({ token: token, sessionId: _lobbySessionId })
                    }).then(() => {
                        if (lobbyOverlay) lobbyOverlay.style.display = 'flex';
                        lobbyShowWaiting();
                        lobbyStartPolling();
                        
                        // [BATRUN-FIX]: Enforce input block for P2 even if joining in launching phase
                        // FR: Forcer le blocage des inputs pour P2 même si on rejoint en phase launching
                        proceedToStream(true);
                    }).catch(e => {
                        console.error('[Lobby] join error:', e);
                        proceedToStream(false);
                    });
                } else {
                    // EN: No active lobby (game already running), just enter stream
                    // FR: Pas de lobby actif (jeu déjà lancé), entrer simplement dans le stream
                    lobbyPhase = 'none';
                    proceedToStream(false);
                }
                return;
            }

            // EN: First check if a lobby already exists for this game
            // FR: D'abord vérifier si un lobby existe déjà pour ce jeu
            fetch(getRelayUrl('/api/public/lobby/status'), {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify({ token: token, sessionId: _lobbySessionId })
            }).then(res => res.json()).then(data => {
            if (data.phase && data.phase !== 'none' && data.phase !== 'launching' && data.phase !== 'confirm') {
            // EN: Lobby exists in a valid phase — join it (P2+)
            // FR: Le lobby existe dans une phase valide — le rejoindre (P2+)
            console.log('[Lobby] Existing lobby found (phase=' + data.phase + '), joining');
            return fetch(getRelayUrl('/api/public/lobby/join'), {
                        method: 'POST',
                        headers: {'Content-Type': 'application/json'},
                        body: JSON.stringify({ token: token, sessionId: _lobbySessionId })
                    });
                } else if (data.phase === 'launching' || data.phase === 'confirm') {
                // [BATRUN-FORK-v9]: Stale lobby detected (phase=launching/confirm but no game running).
                // This happens when a previous game ended but the lobby wasn't reset server-side.
                // Leave the stale lobby first, then create a fresh one.
                // FR: Lobby périmé détecté (phase=launching/confirm mais pas de jeu en cours).
                // Ça arrive quand un jeu précédent s'est terminé mais le lobby n'a pas été reset côté serveur.
                // Quitter le lobby périmé d'abord, puis en créer un nouveau.
                console.log('[Lobby] Stale lobby detected (phase=' + data.phase + '), leaving and creating fresh lobby');
                return fetch(getRelayUrl('/api/public/lobby/leave'), {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify({ token: token, sessionId: _lobbySessionId })
                }).then(() => fetch(getRelayUrl('/api/public/lobby/create'), {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify({ token: token, gamePath: pendingLaunchPath, sessionId: _lobbySessionId })
                }));
                } else {
                // EN: No lobby — create one (P1)
                // FR: Pas de lobby — en créer un (P1)
                console.log('[Lobby] No existing lobby, creating');
                return fetch(getRelayUrl('/api/public/lobby/create'), {
                        method: 'POST',
                        headers: {'Content-Type': 'application/json'},
                        body: JSON.stringify({ token: token, gamePath: pendingLaunchPath, sessionId: _lobbySessionId })
                    });
                }
            }).then(() => {
                // EN: Hide launch overlay, show lobby overlay
                // FR: Cacher l'overlay de lancement, afficher l'overlay lobby
                const overlay = document.getElementById('gameLaunchOverlay');
                if (overlay) overlay.style.display = 'none';
                const lobbyOverlay = document.getElementById('lobbyOverlay');
                if (lobbyOverlay) lobbyOverlay.style.display = 'flex';
                lobbyShowWaiting();
                lobbyStartPolling();
            }).catch(e => console.error('[Lobby] create/join error:', e));
        }

        // [BATRUN-FORK] Lobby Functions (server-coordinated)
        // FR: Fonctions du lobby (coordonnées par le serveur)

        function lobbyShowWaiting() {
            const t = publicTranslations[currentLang];
            document.getElementById('lobbyWaitingText').innerText = t.lobbyWaiting + ' (v3)';
            document.getElementById('lobbyReadyBtn').innerText = t.lobbyReady;
            document.getElementById('lobbySoloBtn').innerText = t.lobbySolo;
            document.getElementById('lobbyWaitingArea').style.display = 'flex';
            document.getElementById('lobbyConfirmArea').style.display = 'none';
            const banner = document.getElementById('lobbyBanner');
            if (banner) banner.style.display = 'flex';
        }

        function lobbySoloLaunch() {
            const token = localStorage.getItem('batrun_token') || '';
            _lobbyActionCooldown = Date.now() + 800;
            console.log('[Lobby] Solo launch');
            lobbyPhase = 'launching';
            lobbyExecuteLaunch();
            fetch(getRelayUrl('/api/public/lobby/solo'), {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify({ token: token, sessionId: _lobbySessionId })
            }).catch(e => console.error('[Lobby] solo error:', e));
        }

        function lobbyStartPolling() {
            if (_lobbyPollTimer) clearInterval(_lobbyPollTimer);
            _lobbyPollTimer = setInterval(lobbyPollStatus, 500);
            lobbyPollStatus(); // immediate first poll
        }

        function lobbyStopPolling() {
            if (_lobbyPollTimer) { clearInterval(_lobbyPollTimer); _lobbyPollTimer = null; }
        }

        async function lobbyPollStatus() {
            const token = localStorage.getItem('batrun_token') || '';
            try {
                const res = await fetch(getRelayUrl('/api/public/lobby/status'), {
                    method: 'POST',
                    headers: {'Content-Type': 'application/json'},
                    body: JSON.stringify({ token: token, sessionId: _lobbySessionId })
                });
                if (!res.ok) return;
                const data = await res.json();
                // [BATRUN-FORK-v4]: Reduced lobby debug logging — only log when phase changes
                // FR: Réduction des logs debug du lobby — ne logger que quand la phase change
                if (data.phase !== lobbyPhase) console.log('[Lobby] Phase changed:', data.phase, '(was:', lobbyPhase, ')');
                const serverPhase = data.phase || 'none';
                // EN: Only accept server phase if it's equal or ahead of local phase
                // FR: N'accepter la phase serveur que si elle est égale ou en avance sur la phase locale
                const phaseOrder = {'none': 0, 'lobby': 1, 'confirm': 2, 'launching': 3};
                const localOrder = phaseOrder[lobbyPhase] || 0;
                const serverOrder = phaseOrder[serverPhase] || 0;
                if (serverOrder >= localOrder || serverPhase === 'none') {
                    lobbyPhase = serverPhase;
                }
                _isLobbyP1 = data.isP1 === true;
                
                // [BATRUN-FIX]: Capture isRbStream for P2 from lobby data
                // FR: Capturer isRbStream pour le P2 via les données du lobby
                if (data.gamePath === '[RETROBAT_UI]') isRbStream = true;
                
                const t = publicTranslations[currentLang];

                // EN: Update UI based on current phase
                // FR: Mettre à jour l'UI selon la phase actuelle
                if (lobbyPhase === 'none') {
                    isWaitingForInput = false;
                    isWaitingForML = false;
                    lobbyHideAll();
                    return;
                }
                // [BATRUN-FORK-v6]: Update join banner when lobby phase changes
                updateLobbyJoinBanner();
                if (lobbyPhase === 'lobby') {
                    document.getElementById('lobbyWaitingArea').style.display = 'flex';
                    document.getElementById('lobbyConfirmArea').style.display = 'none';
                    const banner = document.getElementById('lobbyBanner');
                    if (banner) banner.style.display = 'flex';
                    
                    const soloBtn = document.getElementById('lobbySoloBtn');
                    if (soloBtn) soloBtn.style.display = _isLobbyP1 ? 'inline-block' : 'none';
                }
                if (lobbyPhase === 'confirm') {
                    document.getElementById('lobbyWaitingArea').style.display = 'none';
                    document.getElementById('lobbyConfirmArea').style.display = 'flex';
                    document.getElementById('lobbyConfirmText').innerText = data.isP1 ? t.lobbyConfirmLaunch : t.lobbyP1Confirm;
                    const banner = document.getElementById('lobbyBanner');
                    if (banner) banner.style.display = 'flex';
                }
                if (lobbyPhase === 'launching') {
                    // [BATRUN-FORK-v4]: Only P1 executes the launch. P2 should just join the existing stream.
                    // This prevents the double-launch race condition where both players call /api/public/launch,
                    // which causes a Moonlight bridge restart and kills P1's stream.
                    // FR: Seul P1 exécute le lancement. P2 doit juste rejoindre le stream existant.
                    // Cela empêche la race condition du double-lancement où les deux joueurs appellent
                    // /api/public/launch, ce qui provoque un redémarrage du bridge Moonlight et kill le stream de P1.
                    if (_isLobbyP1) {
                        lobbyExecuteLaunch();
                    } else {
                        // P2: Join the existing stream. P1 is launching the game — P2 does NOT call /api/public/launch.
                        // FR: P2 rejoint le stream existant. P1 lance le jeu — P2 n'appelle PAS /api/public/launch.
                        lobbyStopPolling();
                        const t = publicTranslations[currentLang];
                        const launchOverlay = document.getElementById('gameLaunchOverlay');
                        const overlayText = document.getElementById('overlayText');
                        const overlayLoader = document.getElementById('overlayLoader');
                        if (launchOverlay) {
                            launchOverlay.style.display = 'flex';
                            if (overlayText) overlayText.innerText = t.lobbyLaunching || 'Lancement du jeu en cours...';
                            if (overlayLoader) overlayLoader.style.display = 'block';
                        }
                        const lobbyOverlay = document.getElementById('lobbyOverlay');
                        if (lobbyOverlay) lobbyOverlay.style.display = 'none';
                        const banner = document.getElementById('lobbyBanner');
                        if (banner) banner.style.display = 'none';
        
                        // P2 must also switch to streaming view if not already streaming
                        // FR: P2 doit aussi passer en vue streaming si pas deja en streaming
                        if (!isStreaming) {
                            isStreaming = true;
                            isRbStream = (data.gamePath === '[RETROBAT_UI]');
                            const lv = document.getElementById('listView');
                            const searchContainer = document.getElementById('publicSearchContainer');
                            const sv = document.getElementById('streamView');
                            const title = document.getElementById('viewTitle');
                            lv.style.display = 'none';
                            searchContainer.style.display = 'none';
                            sv.style.display = 'block';
                            title.innerText = t.viewTitleStream;
        
                            const token = localStorage.getItem('batrun_token') || '';
                            // [BATRUN-HUB]: target is now a machine name alias, not an IP
                            let tIp = '';
                            if (apiBase && apiBase.includes('target=')) {
                                try {
                                    const search = apiBase.split('?')[1];
                                    const params = new URLSearchParams(search);
                                    tIp = params.get('target') || '';
                                } catch(e) {}
                            }
                            const streamUrl = `${window.location.protocol}//${window.location.host}/api/moonlight-auth?token=${encodeURIComponent(token)}&hostId=auto&appId=0${tIp ? '&targetIp='+encodeURIComponent(tIp) : ''}`;
                            const iframe = document.getElementById('streamIframe');
                            batrunStartStream(iframe, streamUrl, () => { setTimeout(() => { iframe.focus(); sv.onclick = () => iframe.focus(); }, 1000); });
                        }
        
                        // Hide launch overlay after 3 seconds (same as P1 in lobbyExecuteLaunch)
                        // EN: Hide launch overlay after 5 seconds (increased safety margin)
                        // FR: Cacher l'overlay de lancement après 5 secondes (marge de sécurité accrue)
                        setTimeout(() => {
                            if (launchOverlay) launchOverlay.style.display = 'none';
                            const iframe = document.getElementById('streamIframe');
                            if (iframe && iframe.contentWindow) {
                                try { iframe.contentWindow.postMessage({type: 'BATRUN_LOBBY_INPUT_BLOCK', blocked: false}, '*'); } catch(e) {}
                            }
                            if (iframe) {
                                iframe.focus();
                                try { window.focus(); iframe.contentWindow.focus(); } catch(e) {}
                                try {
                                    if (iframe.contentWindow && iframe.contentWindow.app) {
                                        iframe.contentWindow.app.onUserInteraction();
                                    }
                                } catch(e) {}
                            }
                        }, 5000);
                    }
                    return;
                }

                // EN: Update player banner from server data
                // FR: Mettre à jour la bannière des joueurs depuis les données du serveur
                lobbyUpdateBannerFromServer(data);
            } catch(e) {
                console.error('[Lobby] poll error:', e);
            }
        }

        function lobbyUpdateBannerFromServer(data) {
            const t = publicTranslations[currentLang];
            const container = document.getElementById('lobbyPlayerSlots');
            if (!container || !data.players) return;

            // EN: Track previous player count to detect new joins
            // FR: Suivre le nombre précédent de joueurs pour détecter les nouvelles connexions
            const prevCount = container.childElementCount;
            const newCount = data.players.length;

            container.innerHTML = '';
            data.players.forEach((p, idx) => {
                const slot = document.createElement('div');
                slot.className = 'lobby-player-slot' + (p.ready ? ' ready' : '');
                const icon = document.createElement('span');
                icon.className = 'lobby-player-icon';
                icon.innerText = '🎮';
                const label = document.createElement('span');
                label.className = 'lobby-player-label';
                label.innerText = 'P' + (idx + 1) + (p.isP1 ? ' ★' : '');
                const status = document.createElement('span');
                status.className = 'lobby-player-status ' + (p.ready ? 'ready' : 'not-ready');
                status.innerText = p.ready ? t.lobbyPlayerReady : t.lobbyPlayerNotReady;
                slot.appendChild(icon);
                slot.appendChild(label);
                slot.appendChild(status);
                container.appendChild(slot);
            });

            // EN: Update waiting text with player count
            // FR: Mettre à jour le texte d'attente avec le nombre de joueurs
            if (lobbyPhase === 'lobby') {
                const readyCount = data.players.filter(p => p.ready).length;
                const waitingText = document.getElementById('lobbyWaitingText');
                if (waitingText) {
                    if (readyCount === newCount && newCount > 1) {
                        waitingText.innerText = t.lobbyWaiting; // will switch to confirm by server
                    } else {
                        waitingText.innerText = t.lobbyWaiting + ' (' + readyCount + '/' + newCount + ' ' + t.lobbyPlayerReady + ')';
                    }
                }
            }
        }

        function lobbyHideAll() {
            lobbyPhase = 'none';
            lobbyStopPolling();
            const lobbyOverlay = document.getElementById('lobbyOverlay');
            if (lobbyOverlay) lobbyOverlay.style.display = 'none';
            const banner = document.getElementById('lobbyBanner');
            if (banner) banner.style.display = 'none';
            updateLobbyJoinBanner();
        }

        async function cancelCurrentSession() {
            const t = publicTranslations[currentLang];
            const isJustLobby = (typeof lobbyPhase !== 'undefined' && (lobbyPhase === 'lobby' || lobbyPhase === 'confirm'));
            if (!isJustLobby) {
                if (!confirm(t.emergencyConfirmDesc || ""Voulez-vous vraiment arrêter la session en cours ?"")) return;
            }
            const token = localStorage.getItem('batrun_token') || '';
            try {
                console.log('[Lobby] Requesting session stop...');
                const res = await fetch(getRelayUrl('/api/host/cancel?token=' + encodeURIComponent(token)), { method: 'POST' });
                if (res.ok) {
                    window.location.reload();
                }
            } catch(e) { console.error('[Lobby] Cancel error:', e); }
        }

        function togglePageFullscreen() {
            if (!document.fullscreenElement) {
                document.documentElement.requestFullscreen().then(() => {
                    localStorage.setItem('batrun_fullscreen', 'true');
                }).catch(err => {
                    console.log(`Error attempting to enable full-screen mode: ${err.message} (${err.name})`);
                });
            } else {
                if (document.exitFullscreen) {
                    document.exitFullscreen().then(() => {
                        localStorage.removeItem('batrun_fullscreen');
                    });
                }
            }
        }

        let _fsAutoInit = false;
        document.addEventListener('click', () => {
            if (!_fsAutoInit && localStorage.getItem('batrun_fullscreen') === 'true') {
                _fsAutoInit = true;
                if (!document.fullscreenElement) {
                    document.documentElement.requestFullscreen().catch(err => console.log(err));
                }
            }
        });
        document.addEventListener('keydown', () => {
            if (!_fsAutoInit && localStorage.getItem('batrun_fullscreen') === 'true') {
                _fsAutoInit = true;
                if (!document.fullscreenElement) {
                    document.documentElement.requestFullscreen().catch(err => console.log(err));
                }
            }
        });

        function logout() {
            localStorage.removeItem('batrun_token');
            window.location.reload();
        }

        window.addEventListener('beforeunload', () => {
            // [BATRUN-v3]: Automatic cleanup if user closes the tab during a session
            // FR: Nettoyage automatique si l'utilisateur ferme l'onglet pendant une session
            if (isStreaming || isRbStream || (typeof lobbyPhase !== 'undefined' && lobbyPhase !== 'none')) {
                const token = localStorage.getItem('batrun_token') || '';
                const url = getRelayUrl('/api/host/cancel?token=' + encodeURIComponent(token));
                if (navigator.sendBeacon) {
                    navigator.sendBeacon(url);
                } else {
                    const xhr = new XMLHttpRequest();
                    xhr.open('POST', url, false);
                    xhr.send();
                }
            }
        });

        // [BATRUN-FORK]: Add keyboard support for the lobby (Escape/Backspace to cancel)
        // FR: Ajouter le support du clavier pour le lobby (Echap/Retour arrière pour annuler)
        window.addEventListener('keydown', (e) => {
            if (typeof lobbyPhase !== 'undefined' && (lobbyPhase === 'lobby' || lobbyPhase === 'confirm')) {
                if (e.key === 'Escape' || e.key === 'Backspace') {
                    console.log('[Lobby] -> cancel action (Keyboard)');
                    cancelCurrentSession();
                }
            }
        });
        
        // [BATRUN-FORK-v6]: Show/hide the lobby join banner below the search bar.
        // Visible when a lobby is active OR a game is in progress. Hidden otherwise.
        // FR: Afficher/masquer la bannière de rejoindre le lobby sous la barre de recherche.
        // Visible quand un lobby est actif OU un jeu est en cours. Masquée sinon.
        function updateLobbyJoinBanner() {
            const banner = document.getElementById('lobbyJoinBanner');
            if (!banner) return;
            const _la = (typeof lobbyPhase !== 'undefined' && lobbyPhase !== 'none');
            // EN: Only show banner if web game is in progress OR a lobby is waiting
            // FR: N'afficher le bandeau que si un jeu web est en cours OU qu'un lobby attend
            const _gb = globalStatus && ((globalStatus.isGameInProgress && globalStatus.isWebLaunch) || globalStatus.isLobbyWaiting);
            const t = publicTranslations[currentLang];
            const textEl = document.getElementById('lobbyJoinBannerText');
            const btnEl = document.getElementById('lobbyJoinBannerBtn');
            if (_la || _gb) {
                const titleText = (globalStatus && globalStatus.isLobbyWaiting) ? (t.lobbyActive || 'Lobby en cours') : (t.lobbyJoinBannerTitle || '🎮 Lobby en cours');
                if (textEl) textEl.innerText = titleText;
                if (btnEl) btnEl.innerText = t.lobbyJoinBtn || 'Rejoindre';
                banner.style.display = 'flex';
            } else {
                banner.style.display = 'none';
            }
        }

        // [BATRUN-v3] Show emergency launch button when ES is down
        function showEmergencyRbButton() {
            const container = document.getElementById('publicGamesList');
            const t = publicTranslations[currentLang];
            container.innerHTML = `
                <div style=""grid-column: 1/-1; text-align: center; padding: 40px 20px; animation: fadeIn 0.5s ease-out;"">
                    <p style=""font-size: 1.2rem; margin-bottom: 20px; color: #ff9f43; font-weight: 600;"">⚠️ ${t.errGameServerDown}</p>
                    <button class=""emergency-rb-btn"" onclick=""executeEmergencyRbLaunch()"">
                        🚀 ${t.btnStartGameServer}
                    </button>
                </div>
            `;
            // EN: Refresh focusables to include the new button / FR: Rafraîchir les focusables pour inclure le nouveau bouton
            if (_gpActive) gpSetFocus(getPubFocusables().findIndex(el => el.classList.contains('emergency-rb-btn')));
        }

        async function executeEmergencyRbLaunch() {
            const t = publicTranslations[currentLang];
            const btn = document.querySelector('.emergency-rb-btn');
            if (btn) {
                btn.disabled = true;
                btn.innerText = t.msgLoadingRb || ""Démarrage..."";
            }
            
            const token = localStorage.getItem('batrun_token') || '';
            try {
                await fetch(getRelayUrl('/api/public/start-rb'), {
                    method: 'POST',
                    headers: {'Content-Type': 'application/json'},
                    body: JSON.stringify({ action: 'start_rb', token: token })
                });
                
                // EN: Periodic check to see if ES came back online
                // FR: Vérification périodique pour voir si ES est revenu en ligne
                let checks = 0;
                const checkInterval = setInterval(async () => {
                    checks++;
                    try {
                        const statusRes = await fetch(getRelayUrl('/api/es/status'));
                        const status = await statusRes.json();
                        if (status.isOnline || checks > 15) {
                            clearInterval(checkInterval);
                            searchGames(); // EN: Reload games once online / FR: Recharger les jeux une fois en ligne
                        }
                    } catch(e) {}
                }, 2000);

            } catch(e) {
                console.error('Failed to start RB', e);
                if (btn) {
                    btn.disabled = false;
                    btn.innerText = t.btnStartGameServer;
                }
            }
        }
        
        // EN: Track gamepad state for lobby — detect button RELEASE as action trigger
        // FR: Suivi de l'état des manettes pour le lobby — détecter le RELÂCHEMENT comme déclencheur
        let _lobbyPrevButtons = {};
        let _lobbyPollDebugLogged = false;
        let _isLobbyP1 = false;
        // [BATRUN-FORK-v7]: Track last time we sent input block to iframe.
        // The initial postMessage may be lost if the iframe has not loaded yet,
        // so we periodically re-send BATRUN_LOBBY_INPUT_BLOCK while the lobby is active.
        // FR: Suivi du dernier envoi du blocage d'inputs a l'iframe.
        // Le postMessage initial peut etre perdu si l'iframe n'est pas encore chargee,
        // donc on renvoie periodiquement BATRUN_LOBBY_INPUT_BLOCK tant que le lobby est actif.
        let _lobbyInputBlockLastSent = 0;
        
        function lobbyPollGamepads() {
          if (lobbyPhase === 'none' && !isWaitingForInput) return;
        const now = Date.now();
        let gps = navigator.getGamepads ? [...navigator.getGamepads()] : [];
        try {
            const _lpIframe = document.getElementById('streamIframe');
            if (_lpIframe && _lpIframe.contentWindow && _lpIframe.contentWindow.navigator.getGamepads) {
                gps = gps.concat([..._lpIframe.contentWindow.navigator.getGamepads()]);
            }
        } catch(e) {}
        const token = localStorage.getItem('batrun_token') || '';
        
        // [BATRUN-FORK-v7]: Re-send input block to iframe every ~1s while lobby is active or launching.
        // This ensures the Moonlight iframe receives the block command even if it was
        // sent before the iframe finished loading, preventing spurious 'Start' inputs to EmulationStation.
        // FR: Renvoyer le blocage d'inputs a l'iframe toutes les ~1s tant que le lobby est actif ou en lancement.
        // Cela garantit que l'iframe Moonlight recoit la commande de blocage meme si
        // elle a ete envoyee avant que l'iframe ait fini de charger, evitant les inputs 'Start' intempestifs.
        if (now - _lobbyInputBlockLastSent > 1000) {
            const _lpIframe = document.getElementById('streamIframe');
            if (_lpIframe && _lpIframe.contentWindow) {
                try { _lpIframe.contentWindow.postMessage({type: 'BATRUN_LOBBY_INPUT_BLOCK', blocked: true}, '*'); } catch(e) {}
            }
            _lobbyInputBlockLastSent = now;
        }

        if (lobbyPhase === 'launching' || isWaitingForInput) return;
        

        if (!_lobbyPollDebugLogged) {
        _lobbyPollDebugLogged = true;
                const gpCount = gps.filter(g => g).length;
                console.log('[Lobby] lobbyPollGamepads active, phase=' + lobbyPhase + ', gamepads=' + gpCount);
            }

            for (let i = 0; i < gps.length; i++) {
                const gp = gps[i];
                if (!gp) continue;

                const prev = _lobbyPrevButtons[i] || {};

                // EN: Detect button RELEASE (was pressed, now released) — this is the action trigger
                // FR: Détecter le RELÂCHEMENT (était pressé, maintenant relâché) — c'est le déclencheur d'action
                const startWasHeld = prev[9] === true;
                const startIsDown = gp.buttons[9] && gp.buttons[9].pressed;
                const startReleased = startWasHeld && !startIsDown;

                const xWasHeld = prev[2] === true;
                const xIsDown = gp.buttons[2] && gp.buttons[2].pressed;
                const xReleased = xWasHeld && !xIsDown;

                const bWasHeld = prev[1] === true;
                const bIsDown = gp.buttons[1] && gp.buttons[1].pressed;
                const bReleased = bWasHeld && !bIsDown;

                // EN: Also detect new press as fallback (if button pressed very quickly)
                // FR: Détecter aussi le nouveau pressage comme fallback (si le bouton est pressé très rapidement)
                const startNewPress = startIsDown && !startWasHeld;
                const xNewPress = xIsDown && !xWasHeld;
                const bNewPress = bIsDown && !bWasHeld;

                // EN: ALWAYS save current state for next frame — even during cooldown
                // FR: TOUJOURS sauvegarder l'état pour la prochaine frame — même pendant le cooldown
                const cur = {};
                gp.buttons.forEach((btn, bi) => cur[bi] = btn.pressed);
                _lobbyPrevButtons[i] = cur;

                // EN: Action cooldown to prevent double-trigger (but state was already saved above)
                // FR: Cooldown d'action pour éviter le double-déclenchement (l'état est déjà sauvegardé ci-dessus)
                if (now < _lobbyActionCooldown) continue;

                const startAction = startReleased || startNewPress;
                const xAction = xReleased || xNewPress;
                const bAction = bReleased || bNewPress;

                if (startAction) console.log('[Lobby] START action on gp' + i + ' phase=' + lobbyPhase + ' release=' + startReleased + ' newPress=' + startNewPress);
                if (xAction) console.log('[Lobby] X action on gp' + i + ' phase=' + lobbyPhase + ' release=' + xReleased + ' newPress=' + xNewPress);

                if (lobbyPhase === 'lobby') {
                    // EN: START = mark ready, X = solo launch (don't wait)
                    // FR: START = marquer prêt, X = lancement solo (ne pas attendre)
                    if (startAction) {
                        _lobbyActionCooldown = now + 400;
                        console.log('[Lobby] -> ready action');
                        fetch(getRelayUrl('/api/public/lobby/ready'), {
                            method: 'POST',
                            headers: {'Content-Type': 'application/json'},
                            body: JSON.stringify({ token: token, sessionId: _lobbySessionId })
                        }).then(() => lobbyPollStatus()).catch(e => console.error('[Lobby] ready error:', e)); // Force instant sync
                    } else if (xAction && _isLobbyP1) {
                        _lobbyActionCooldown = now + 400;
                        console.log('[Lobby] -> solo action');
                        lobbySoloLaunch();
                    } else if (bAction) {
                        _lobbyActionCooldown = now + 400;
                        console.log('[Lobby] -> cancel action');
                        cancelCurrentSession();
                    }
                } else if (lobbyPhase === 'confirm') {
                    // EN: P1 confirms launch with START
                    // FR: P1 confirme le lancement avec START
                    if (startAction && _isLobbyP1) {
                        _lobbyActionCooldown = now + 400;
                        console.log('[Lobby] -> launch action');
                        fetch(getRelayUrl('/api/public/lobby/launch'), {
                            method: 'POST',
                            headers: {'Content-Type': 'application/json'},
                            body: JSON.stringify({ token: token, sessionId: _lobbySessionId })
                        }).then(res => res.json()).then(data => {
                            if (data.success) lobbyExecuteLaunch();
                        }).catch(e => console.error('[Lobby] launch error:', e));
                    }
                }
            }
            
            // [BATRUN-v3] Handle Emergency RB Button click via Gamepad
            const els = getPubFocusables();
            const focusedEl = els[_gpFocusedIndex];
            if (focusedEl && focusedEl.classList.contains('emergency-rb-btn')) {
                const gp = gps[0]; // Primary gamepad
                if (gp) {
                    const prev = _lobbyPrevButtons[gp.index] || {};
                    const aWasHeld = prev[0] === true;
                    const aIsDown = gp.buttons[0] && gp.buttons[0].pressed;
                    if (aWasHeld && !aIsDown) {
                        executeEmergencyRbLaunch();
                    }
                }
            }
        }

        function lobbyExecuteLaunch() {
            lobbyPhase = 'launching';
            isWaitingForML = true;
            waitStartTimestamp = Date.now();
            lobbyStopPolling();

            // EN: Show ""game launching"" overlay to hide ES interface while game starts
            // FR: Afficher l'overlay ""lancement du jeu"" pour cacher l'interface ES pendant le démarrage
            const t = publicTranslations[currentLang];
            const launchOverlay = document.getElementById('gameLaunchOverlay');
            const overlayText = document.getElementById('overlayText');
            const overlayLoader = document.getElementById('overlayLoader');
            if (launchOverlay) {
                launchOverlay.style.display = 'flex';
                if (overlayText) overlayText.innerText = t.lobbyLaunching || 'Lancement du jeu en cours...';
                if (overlayLoader) overlayLoader.style.display = 'block';
            }

            // EN: Hide lobby overlay and banner
            // FR: Cacher l'overlay et la bannière du lobby
            const lobbyOverlay = document.getElementById('lobbyOverlay');
            if (lobbyOverlay) lobbyOverlay.style.display = 'none';
            const banner = document.getElementById('lobbyBanner');
            if (banner) banner.style.display = 'none';

            // EN: Execute the actual game launch
            // FR: Exécuter le lancement réel du jeu
            executeRealLaunch(pendingLaunchPath);
            pendingLaunchPath = null;

            // EN: Wait 5 seconds for the game to start on ES before unblocking inputs (safety margin)
            // FR: Attendre 5 secondes pour que le jeu démarre sur ES avant de débloquer les inputs (marge de sécurité)
            const iframe = document.getElementById('streamIframe');
            setTimeout(() => {
                // EN: Hide launch overlay, unblock gamepad inputs
                // FR: Cacher l'overlay de lancement, débloquer les inputs manette
                if (launchOverlay) launchOverlay.style.display = 'none';

                if (iframe && iframe.contentWindow) {
                    try { iframe.contentWindow.postMessage({type: 'BATRUN_LOBBY_INPUT_BLOCK', blocked: false}, '*'); } catch(e) {}
                }
                lobbyPhase = 'none';

                if (iframe) {
                    iframe.focus();
                    try { window.focus(); iframe.contentWindow.focus(); } catch(e) {}
                    try {
                        if (iframe.contentWindow && iframe.contentWindow.app) {
                            iframe.contentWindow.app.onUserInteraction();
                        }
                    } catch(e) {}
                }
            }, 3000);
        }

        function pollLaunchInput() {
            if (isWaitingForInput) {
                let gps = navigator.getGamepads ? [...navigator.getGamepads()] : [];
                try {
                    const _lpIframe = document.getElementById('streamIframe');
                    if (_lpIframe && _lpIframe.contentWindow && _lpIframe.contentWindow.navigator.getGamepads) {
                        gps = gps.concat([..._lpIframe.contentWindow.navigator.getGamepads()]);
                    }
                } catch(e) {}
                for (let i = 0; i < gps.length; i++) {
                    const gp = gps[i];
                    if (!gp) continue;
                    if (gp.buttons.some(b => b.pressed)) {
                        triggerLaunch();
                        break;
                    }
                }
            }
            // [BATRUN-FORK]: Poll lobby gamepad input when in lobby phase OR waiting for first input
            // FR: Scruter les entrées manette pendant la phase lobby OU l'attente du premier input
            if ((lobbyPhase !== 'none' && lobbyPhase !== 'launching') || isWaitingForInput) {
                lobbyPollGamepads();
            }
            requestAnimationFrame(pollLaunchInput);
        }
        requestAnimationFrame(pollLaunchInput);

        document.addEventListener('keydown', (e) => {
            if (isWaitingForInput && (e.key === 'Enter' || e.key === ' ')) triggerLaunch();
            // [BATRUN-FORK]: Keyboard support for lobby phases
            // FR: Support clavier pour les phases du lobby
            if (lobbyPhase === 'lobby') {
                if (e.key === 'Enter' || e.key === ' ') {
                    const token = localStorage.getItem('batrun_token') || '';
                    fetch(getRelayUrl('/api/public/lobby/ready'), {
                        method: 'POST',
                        headers: {'Content-Type': 'application/json'},
                        body: JSON.stringify({ token: token, sessionId: _lobbySessionId })
                    }).then(() => lobbyPollStatus()).catch(e => console.error('[Lobby] ready error:', e));
                } else if ((e.key === 'Escape' || e.key === 'x' || e.key === 'X') && _isLobbyP1) {
                    lobbySoloLaunch();
                }
            } else if (lobbyPhase === 'confirm') {
                if ((e.key === 'Enter' || e.key === ' ') && _isLobbyP1) {
                    const token = localStorage.getItem('batrun_token') || '';
                    fetch(getRelayUrl('/api/public/lobby/launch'), {
                        method: 'POST',
                        headers: {'Content-Type': 'application/json'},
                        body: JSON.stringify({ token: token, sessionId: _lobbySessionId })
                    }).then(res => res.json()).then(data => {
                        if (data.success) lobbyExecuteLaunch();
                    }).catch(e => console.error('[Lobby] launch error:', e));
                }
            }
        });
        document.addEventListener('click', (e) => {
            if (isWaitingForInput) triggerLaunch();
        });

        async function executeRealLaunch(path) {
            const token = localStorage.getItem('batrun_token');
            const t = publicTranslations[currentLang];
            try {
                const res = await fetch(getRelayUrl('/api/public/launch'), {
                    method: 'POST',
                    headers: {'Content-Type': 'application/json'},
                    body: JSON.stringify({ token: token, gamePath: path })
                });
                // EN: 409 Conflict = game already running, not a real error (we join the existing session).
                //     200 with alreadyRunning=true = same semantic, cleaner. Never show alert for these.
                // FR: 409 Conflict = jeu déjà en cours, pas une erreur réelle (on rejoint la session existante).
                //     200 avec alreadyRunning=true = même sémantique, plus propre. Ne jamais afficher d'alerte.
                if (!res.ok && res.status !== 409) { 
                    console.error('[Launch] HTTP error ' + res.status + ' when launching ' + path);
                }
            } catch (e) {
                console.error('Launch execution error', e);
            }
        }
        
        // [BATRUN-FORK-v4]: Join an existing game session without launching a new game.
        // This is used by P2 (or any late-joiner) when a game is already in progress.
        // It creates the Moonlight iframe to watch the existing stream, but does NOT call /api/public/launch.
        // FR: Rejoindre une session de jeu existante sans lancer un nouveau jeu.
        // Utilise par P2 (ou tout retardataire) quand un jeu est deja en cours.
        // Cree l'iframe Moonlight pour regarder le stream existant, mais n'appelle PAS /api/public/launch.
        function joinExistingSession() {
            closeGameDetails();
            if (isStreaming) return; // Already streaming, nothing to do
            if (!globalStatus || !globalStatus.isMoonlightEnabled) return;
        
            isStreaming = true;
            isRbStream = false;
            
            // [BATRUN-FORK-v11]: Request gamepad initialization flow by setting isWaitingForML.
            // This ensures the ""Press any button"" overlay appears and the user can go fullscreen.
            // FR: Demander le flux d'initialisation de la manette en activant isWaitingForML.
            // Cela garantit l'affichage de l'overlay ""Appuyez sur un bouton"" et le passage en plein écran.
            isWaitingForML = true;
            _sessionGameStarted = false;
            waitStartTimestamp = Date.now();
            pendingLaunchPath = '[JOIN]'; 

            const sv = document.getElementById('streamView');
            if (sv) sv.style.display = 'block';

            // EN: Fullscreen is now handled by triggerLaunch() to ensure it works on all devices via user gesture.
            // FR: Le plein écran est maintenant géré par triggerLaunch() pour garantir son fonctionnement via geste utilisateur.
            
            // [BATRUN-FORK-v10]: If joining while a lobby is active, start lobby polling to show overlay
            // FR: Si on rejoint pendant qu'un lobby est actif, démarrer le polling pour afficher l'overlay
            if (globalStatus && globalStatus.isLobbyActive) {
                lobbyStartPolling();
            }

            const lv = document.getElementById('listView');
            const searchContainer = document.getElementById('publicSearchContainer');
            const title = document.getElementById('viewTitle');
            const t = publicTranslations[currentLang];
            lv.style.display = 'none';
            searchContainer.style.display = 'none';
            sv.style.display = 'block';
            title.innerText = t.viewTitleStream;
        
            const overlay = document.getElementById('gameLaunchOverlay');
            const overlayText = document.getElementById('overlayText');
            const overlayLoader = document.getElementById('overlayLoader');
            if (overlay) {
                overlay.style.display = 'flex';
                if (overlayText) overlayText.innerText = t.msgConnecting || 'Connexion en cours...';
                if (overlayLoader) overlayLoader.style.display = 'block';
            }
        
            const token = localStorage.getItem('batrun_token') || '';
            // [BATRUN-HUB]: target is now a machine name alias, not an IP
            let tIp = '';
            if (apiBase && apiBase.includes('target=')) {
                try {
                    const search = apiBase.split('?')[1];
                    const params = new URLSearchParams(search);
                    tIp = params.get('target') || '';
                } catch(e) {}
            }
            const streamUrl = `${window.location.protocol}//${window.location.host}/api/moonlight-auth?token=${encodeURIComponent(token)}&hostId=auto&appId=0${tIp ? '&targetIp='+encodeURIComponent(tIp) : ''}`;
            const iframe = document.getElementById('streamIframe');
            batrunStartStream(iframe, streamUrl, () => { setTimeout(() => { iframe.focus(); sv.onclick = () => iframe.focus(); }, 1000); });
        }
        
        function setLanguage(lang) {
            currentLang = typeof publicTranslations[lang] !== 'undefined' ? lang : 'en';
            // EN: Persist language choice across pages and refreshes
            // FR: Persister le choix de langue entre les pages et les rafraichissements
            localStorage.setItem('batrun_lang', currentLang);
            document.querySelectorAll('.flag').forEach(f => f.classList.remove('active'));
            
            // EN: Update active state for both header and auth flags
            // FR: Mettre à jour l'état actif pour les drapeaux du header et du login
            const activeFlags = [
                document.getElementById('flag-' + currentLang),
                document.getElementById('flag-' + currentLang + '-auth')
            ];
            activeFlags.forEach(f => { if(f) f.classList.add('active'); });

            const t = publicTranslations[currentLang];
            // EN: Null-safe access for auth elements (not present on cloud page)
            // FR: Acces null-safe pour les elements auth (absents de la page cloud)
            const tabL = document.getElementById('tabLogin'); if(tabL) tabL.innerText = t.tabLogin;
            const tabR = document.getElementById('tabRegister'); if(tabR) tabR.innerText = t.tabRegister;
            const uField = document.getElementById('username'); if(uField) uField.placeholder = t.username;
            const pField = document.getElementById('password'); if(pField) pField.placeholder = t.password;
            const subBtn = document.getElementById('submitBtn'); if(subBtn) subBtn.innerText = isLogin ? t.btnConnect : t.btnRegister;
            const btnG = document.getElementById('btnGuestStart');
            if(btnG) btnG.innerText = t.btnGuestStart;
            const lnkAdmin = document.getElementById('lnkAdmin');
            if(lnkAdmin) lnkAdmin.innerText = t.adminLink;
            const logBtn = document.getElementById('logoutBtn'); if(logBtn) logBtn.innerText = t.btnLogout;
            const btnStopSession = document.getElementById('btnStopSessionLobby');
            if (btnStopSession) btnStopSession.innerText = t.lobbyStopBtn || ""Stop Session"";
            const txtLoadingGames = document.getElementById('txtLoadingGames');
            if(txtLoadingGames) txtLoadingGames.innerText = t.txtLoadingGames;
            const liveEl = document.getElementById('txtLiveStream'); if(liveEl) liveEl.innerText = t.txtLiveStream || ""LIVE STREAM"";
            const stopEl = document.getElementById('btnStopGame'); if(stopEl) stopEl.innerText = t.btnStopGame || ""Stop Game"";
            const btnFullStream = document.getElementById('btnFullStream');
            if (btnFullStream) btnFullStream.innerText = t.btnFullscreen;

            if (!isStreaming && document.getElementById('viewTitle').innerText.includes('BatRun')) {
                document.getElementById('viewTitle').innerText = t.viewTitleIdle;
            } else if (isStreaming) {
                document.getElementById('viewTitle').innerText = t.viewTitleStream;
            }

            const searchInput = document.getElementById('gameSearchInput');
            if(searchInput) searchInput.placeholder = t.searchPlaceholder;
            const systemFilter = document.getElementById('systemFilter');
            if(systemFilter && systemFilter.options.length > 0) systemFilter.options[0].innerText = t.sysFilterAll;
            const customSystemSelect = document.getElementById('customSystemSelect');
            if (customSystemSelect) customSystemSelect.innerText = t.sysFilterAll;
            const sysModalTitle = document.getElementById('sysModalTitle');
            if (sysModalTitle) sysModalTitle.innerText = t.sysModalTitle;

            const customGenreSelect = document.getElementById('customGenreSelect');
            if (customGenreSelect) customGenreSelect.innerText = t.genreFilterAll;
            const genreModalTitle = document.getElementById('genreModalTitle');
            if (genreModalTitle) genreModalTitle.innerText = t.genreModalTitle;

            const customSortSelect = document.getElementById('customSortSelect');
            if (customSortSelect) customSortSelect.innerText = typeof currentSort !== 'undefined' && currentSort === 'system' ? t.sortSystem : t.sortAlpha;
            const sortModalTitle = document.getElementById('sortModalTitle');
            if (sortModalTitle) sortModalTitle.innerText = t.sortModalTitle;
            const sortOptAlpha = document.getElementById('sortOptAlpha');
            if (sortOptAlpha) sortOptAlpha.innerText = t.sortAlpha;
            const sortOptSystem = document.getElementById('sortOptSystem');
            if (sortOptSystem) sortOptSystem.innerText = t.sortSystem;

            const btnHistoryToggle = document.getElementById('btnHistoryToggle');
            if (btnHistoryToggle) btnHistoryToggle.innerText = typeof showHistoryOnly !== 'undefined' && showHistoryOnly ? t.historyBtnActive : t.historyBtn;
            
            // Force reload of systems grid on next open to apply language
            systemsLoaded = false;
            
            const btnOpenRbModal = document.getElementById('btnOpenRbModal');
            if (btnOpenRbModal) btnOpenRbModal.innerText = t.btnRb;
            const rbTitle = document.getElementById('rbModalTitle');
            if (rbTitle) rbTitle.innerText = t.rbModalTitle;
            const rbDesc = document.getElementById('rbModalDesc');
            if (rbDesc) rbDesc.innerText = t.rbModalDesc;
            const rbCancel = document.getElementById('btnRbCancel');
            if (rbCancel) rbCancel.innerText = t.rbCancel;
            const rbLaunch = document.getElementById('btnRbConfirm');
            if (rbLaunch) rbLaunch.innerText = t.rbLaunch;

            // [BATRUN-FORK-v6]: Update lobby join banner text when language changes
            updateLobbyJoinBanner();
            
            // EN: If games are loaded, update their buttons and empty messages
            const container = document.getElementById('publicGamesList');
            if(container && container.innerHTML.includes('opacity:0.5') && !container.innerHTML.includes('Chargement') && !container.innerHTML.includes('Loading')) {
                container.innerHTML = `<p style=""opacity:0.5; grid-column: 1/-1;"">${t.noGames}</p>`;
            } else if (container) {
                Array.from(container.getElementsByClassName('play-btn')).forEach(btn => {
                    if(!btn.classList.contains('disabled-btn')) btn.innerText = t.btnStart;
                    else { btn.title = t.playBusy; /* keep lock icon */ }
                });
            }
            const infTrigger = document.getElementById('infiniteScrollTrigger');
            if(infTrigger) infTrigger.innerHTML = `<span>${t.loadingWait}</span>`;
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

        function showMainUI() {
            // EN: authPage does not exist on cloud page (separated into ConnectPage.cs)
            // FR: authPage n'existe pas sur la page cloud (separe dans ConnectPage.cs)
            const _ap = document.getElementById(""authPage""); if(_ap) _ap.style.display = ""none"";
            
            // [BATRUN-FIX]: Reset Gamepad state during transition to prevent Button Bleed
            // EN: Capture CURRENT button states instead of clearing _gpLastButtons.
            // Clearing to {} causes pressed() to see prev[i]=undefined, so !undefined=true,
            // so any still-held button is detected as a NEW press, auto-clicking elements on the new page.
            // FR: Capturer l'état ACTUEL des boutons au lieu de vider _gpLastButtons.
            // Vider vers {} fait que pressed() voit prev[i]=undefined → !undefined=true,
            // donc tout bouton encore maintenu est détecté comme un NOUVEAU appui, cliquant automatiquement des éléments.
            _gpActive = false;
            _gpFocusedIndex = -1; // EN: Reset focus index to prevent clicking wrong element after transition
            const _gps = navigator.getGamepads ? navigator.getGamepads() : [];
            for (const _gp of _gps) {
                if (!_gp) continue;
                const _cur = {};
                _gp.buttons.forEach((_btn, _i) => _cur[_i] = _btn.pressed);
                _cur.ax0 = _gp.axes[0] || 0;
                _cur.ax1 = _gp.axes[1] || 0;
                _gpLastButtons[_gp.index] = _cur;
            }
            if (document.activeElement) document.activeElement.blur();
            _activeInput = null;

            loadSystems();
            searchGames(); // Initial load
            startPolling();
        }

        let currentSystem = """";
        let currentGenre = """";
        let currentSort = ""game"";
        let showHistoryOnly = false;

        async function loadSystems() {
            if (systemsLoaded) return;
            try {
                const res = await fetch(getRelayUrl('/api/es/systems'));
                if(!res.ok) return;
                const systems = await res.json();
                
                const grid = document.getElementById('systemSelectGrid');
                if(!grid) return;
                
                const t = publicTranslations[currentLang] || {};
                const allText = t.sysFilterAll || ""TOUS LES SYSTÈMES"";
                
                // Option ""Tous les systèmes""
                grid.innerHTML = `<div class=""system-option ${currentSystem === '' ? 'focused' : ''}"" data-val="""" data-disp=""${allText.replace(/""/g, '&quot;')}"" onclick=""selectSystem(this.dataset.val, this.dataset.disp)"">${allText}</div>`;
                
                systems.sort((a,b) => (a.fullname || a.name).localeCompare(b.fullname || b.name)).forEach(s => {
                    const sysName = s.fullname || s.name.toUpperCase();
                    grid.innerHTML += `<div class=""system-option ${currentSystem === s.name ? 'focused' : ''}"" data-val=""${s.name.replace(/""/g, '&quot;')}"" data-disp=""${sysName.replace(/""/g, '&quot;')}"" onclick=""selectSystem(this.dataset.val, this.dataset.disp)"">${sysName}</div>`;
                });
                systemsLoaded = true;
            } catch(e) {}
        }

        
        function openGenreModal() {
            _savedGpIndex = _gpFocusedIndex;
            populateGenreFilter(); // Update grid
            const modal = document.getElementById('genreSelectModal');
            if (modal) modal.style.display = 'flex';
        }

        function closeGenreModal() {
            const modal = document.getElementById('genreSelectModal');
            if (modal) modal.style.display = 'none';
            _gpContext = 'main';
            gpSetFocus(_savedGpIndex);
        }

        function selectGenre(genreStr, displayStr) {
            currentGenre = genreStr;
            const btn = document.getElementById('customGenreSelect');
            if (btn) btn.innerText = displayStr;
            closeGenreModal();
            applyLocalFilters();
        }

        function openSortModal() {
            _savedGpIndex = _gpFocusedIndex;
            populateSortFilter();
            const modal = document.getElementById('sortSelectModal');
            if (modal) modal.style.display = 'flex';
        }

        function closeSortModal() {
            const modal = document.getElementById('sortSelectModal');
            if (modal) modal.style.display = 'none';
            _gpContext = 'main';
            gpSetFocus(_savedGpIndex);
        }

        function selectSort(sortStr, displayStr) {
            currentSort = sortStr;
            const btn = document.getElementById('customSortSelect');
            if (btn) btn.innerText = displayStr;
            closeSortModal();
            applyLocalFilters();
        }

        function populateSortFilter() {
            const grid = document.getElementById('sortSelectGrid');
            if(!grid) return;
            
            const t = publicTranslations[currentLang] || publicTranslations['en'];
            grid.innerHTML = `<div class=""system-option ${currentSort === 'game' ? 'focused' : ''}"" onclick=""selectSort('game', '${t.sortAlpha}')"">${t.sortAlpha}</div>`;
            grid.innerHTML += `<div class=""system-option ${currentSort === 'system' ? 'focused' : ''}"" onclick=""selectSort('system', '${t.sortSystem}')"">${t.sortSystem}</div>`;
        }

        function toggleHistoryFilter() {
            showHistoryOnly = !showHistoryOnly;
            const btn = document.getElementById('btnHistoryToggle');
            const t = publicTranslations[currentLang] || publicTranslations['en'];
            if (btn) {
                if (showHistoryOnly) {
                    btn.classList.add('filter-active');
                    btn.innerText = t.historyBtnActive;
                } else {
                    btn.classList.remove('filter-active');
                    btn.innerText = t.historyBtn;
                }
            }
            applyLocalFilters();
        }

        function addToHistory(path) {
            let history = [];
            try {
                const histStr = localStorage.getItem('batrun_history');
                if (histStr) history = JSON.parse(histStr);
            } catch(e) {}
            
            // Remove if exists to put it at the top
            history = history.filter(p => p !== path);
            history.unshift(path);
            
            // Keep max 100
            if (history.length > 100) history = history.slice(0, 100);
            
            localStorage.setItem('batrun_history', JSON.stringify(history));
        }

        function openSystemModal() {
            _savedGpIndex = _gpFocusedIndex; // Save
            loadSystems();
            const modal = document.getElementById('systemSelectModal');
            if (modal) modal.style.display = 'flex';
        }

        function closeSystemModal() {
            const modal = document.getElementById('systemSelectModal');
            if (modal) modal.style.display = 'none';
            _gpContext = 'main';
            gpSetFocus(_savedGpIndex); // Restore
        }

        function selectSystem(systemId, systemName) {
            currentSystem = systemId;
            const btn = document.getElementById('customSystemSelect');
            if (btn) btn.innerText = systemName;
            
            closeSystemModal();
            searchGames();
        }

        // EN: searchTimer already declared / FR: searchTimer déjà déclaré ci-dessus
        function searchGamesDebounced() {
            if (searchTimer) clearTimeout(searchTimer);
            searchTimer = setTimeout(() => {
                searchGames();
            }, 300);
        }

        async function searchGames() {
            const query = document.getElementById('gameSearchInput').value;
            const system = currentSystem;
            const container = document.getElementById('publicGamesList');
            const t = publicTranslations[currentLang];
            
            container.innerHTML = `<p style=""opacity:0.5; grid-column: 1/-1;"">${t.loadingWait}</p>`;
            
            try {
                const params = `q=${encodeURIComponent(query)}&system=${encodeURIComponent(system)}`;
                const res = await fetch(getRelayUrl(`/api/es/games?${params}`));
                
                if (!res.ok) {
                    // [BATRUN-v3] Check if ES is down on error
                    const statusRes = await fetch(getRelayUrl('/api/es/status'));
                    if (statusRes.ok) {
                        const status = await statusRes.json();
                        if (!status.isOnline) {
                            showEmergencyRbButton();
                            return;
                        }
                    }
                    throw new Error('Network response was not ok');
                }
                
                allFetchedGames = await res.json();
                
                populateGenreFilter();
                applyLocalFilters();
            } catch(e) {
                // [BATRUN-v3] Fallback to status check on any failure
                try {
                    const statusRes = await fetch(getRelayUrl('/api/es/status'));
                    if (statusRes.ok) {
                        const status = await statusRes.json();
                        if (!status.isOnline) {
                            showEmergencyRbButton();
                            return;
                        }
                    }
                } catch(e2) {}
                container.innerHTML = `<p style=""opacity:0.5; grid-column: 1/-1; color:var(--danger)"">${t.errConn}</p>`;
            }
        }

        
        function populateGenreFilter() {
            const grid = document.getElementById('genreSelectGrid');
            if (!grid) return;
            
            const genres = new Set();
            allFetchedGames.forEach(g => {
                if (g.genre) {
                    genres.add(g.genre.trim());
                }
            });
            
            const t = publicTranslations[currentLang] || {};
            const allStr = t.allGenres || ""TOUS LES GENRES"";
            
            let html = `<div class=""system-option ${currentGenre === '' ? 'focused' : ''}"" data-val="""" data-disp=""${allStr.replace(/""/g, '&quot;')}"" onclick=""selectGenre(this.dataset.val, this.dataset.disp)"">${allStr}</div>`;
            
            Array.from(genres).sort().forEach(genre => {
                const isFocused = currentGenre === genre ? 'focused' : '';
                html += `<div class=""system-option ${isFocused}"" data-val=""${genre.replace(/""/g, '&quot;')}"" data-disp=""${genre.replace(/""/g, '&quot;')}"" onclick=""selectGenre(this.dataset.val, this.dataset.disp)"">${genre}</div>`;
            });
            
            grid.innerHTML = html;
        }

        function applyLocalFilters() {
            const genre = currentGenre;
            const sortType = currentSort;
            
            let filtered = allFetchedGames;
            if (genre) {
                filtered = filtered.filter(g => g.genre && g.genre.trim() === genre);
            }
            
            if (showHistoryOnly) {
                let history = [];
                try {
                    const histStr = localStorage.getItem('batrun_history');
                    if (histStr) history = JSON.parse(histStr);
                } catch(e) {}
                
                filtered = filtered.filter(g => history.includes(g.path));
                
                // Sort by history order
                filtered.sort((a, b) => history.indexOf(a.path) - history.indexOf(b.path));
            }
            
            filtered.sort((a, b) => {
                if (sortType === 'system') {
                    const sysCmp = (a.system || '').localeCompare(b.system || '');
                    if (sysCmp !== 0) return sysCmp;
                    return (a.name || '').localeCompare(b.name || '');
                } else {
                    return (a.name || '').localeCompare(b.name || '');
                }
            });

            currentGamesList = filtered;
            renderPage = 0;
            
            const container = document.getElementById('publicGamesList');
            container.innerHTML = '';
            
            if (currentGamesList.length === 0) {
                const t = publicTranslations[currentLang] || {};
                // [BATRUN-v3] Even if list is empty, check if ES is online
                fetch(getRelayUrl('/api/es/status')).then(res => res.json()).then(status => {
                    if (!status.isOnline) {
                        showEmergencyRbButton();
                    } else {
                        container.innerHTML = `<p style=""opacity:0.5; grid-column: 1/-1;"">${t.noGames || ""Aucun jeu trouvé""}</p>`;
                    }
                }).catch(() => {
                    container.innerHTML = `<p style=""opacity:0.5; grid-column: 1/-1;"">${t.noGames || ""Aucun jeu trouvé""}</p>`;
                });
                return;
            }

            renderGamesChunk();
        }

        function renderGamesChunk() {
            const container = document.getElementById('publicGamesList');
            const t = publicTranslations[currentLang];
            const start = renderPage * RENDER_CHUNK;
            const end = Math.min(start + RENDER_CHUNK, currentGamesList.length);
            
            // Clean up old trigger
            const oldTrigger = document.getElementById('infiniteScrollTrigger');
            if (oldTrigger) {
                if (scrollObserver) scrollObserver.unobserve(oldTrigger);
                oldTrigger.remove();
            }
            
            const fragment = document.createDocumentFragment();

            for (let i = start; i < end; i++) {
                const g = currentGamesList[i];
                const item = document.createElement('div');
                item.className = 'game-item';
                item.tabIndex = 0; // EN: Enable keyboard/gamepad focus / FR: Activer focus clavier/manette
                item.style.animationDelay = ((i % RENDER_CHUNK) * 0.02) + 's';
                item.dataset.gameIndex = i; // EN: Store index for gamepad nav / FR: Index pour nav manette
                
                const fallbackImg = g.thumbnail || g.image || g.fanart || g.marquee;
                let imgSrc = fallbackImg ? getRelayUrl('/api/es/media?path=' + encodeURIComponent(fallbackImg)) : '';
                
                const imgContainerHtml = imgSrc 
                    ? `<img src=""${imgSrc}"" loading=""lazy"" onerror=""this.style.display='none'; this.nextElementSibling.style.display='flex';"">` 
                    : '';
                
                const emptyImgHtml = `<div style=""text-align:center; font-size:3rem; aspect-ratio:3/4; background:rgba(0,0,0,0.3); border-radius:8px; display:${imgSrc ? 'none' : 'flex'}; align-items:center; justify-content:center; margin-bottom:8px;"">🎮</div>`;
                
                const sys = document.createElement('div');
                sys.className = 'game-sys';
                sys.innerText = g.system || t.sysFilterUnknown;

                const title = document.createElement('div');
                title.className = 'game-title';
                title.innerText = g.name || t.sysFilterUnknown;
                
                item.innerHTML = imgContainerHtml + emptyImgHtml;
                item.appendChild(sys);
                item.appendChild(title);

                // EN: Click opens detail modal / FR: Clic ouvre la fiche détaillée
                item.onclick = () => showGameDetails(g);
                item.onkeydown = (e) => { if(e.key === 'Enter' || e.key === ' ') { e.preventDefault(); showGameDetails(g); } };
                
                fragment.appendChild(item);
            }
            
            container.appendChild(fragment);
            
            if (end < currentGamesList.length) {
                const trigger = document.createElement('div');
                trigger.id = 'infiniteScrollTrigger';
                trigger.style = ""grid-column: 1/-1; height: 50px; display: flex; align-items: center; justify-content: center; opacity: 0.5; font-size: 0.8rem;"";
                trigger.innerHTML = `<span>${t.loadingWait}</span>`;
                container.appendChild(trigger);

                if (!scrollObserver) {
                    scrollObserver = new IntersectionObserver((entries) => {
                        if (entries[0].isIntersecting) {
                            renderPage++;
                            renderGamesChunk();
                        }
                    }, { rootMargin: '200px' });
                }
                scrollObserver.observe(trigger);
            }
        }

        async function launchGame(path) {
            // EN: Reset session game started flag for new game / FR: Réinitialiser le flag de jeu démarré pour le nouveau jeu
            _sessionGameStarted = false;
            addToHistory(path);
            
            // EN: If streaming is disabled, launch the game directly on host without switching views
            // FR: Si le streaming est désactivé, lancer le jeu directement sur l'hôte sans changer de vue
            if (globalStatus && !globalStatus.isMoonlightEnabled) {
                executeRealLaunch(path);
                closeGameDetails();
                return;
            }

            // [BATRUN-FORK]: Reversing launch logic. 
            // 1. Show stream view + Start Moonlight 
            // 2. Wait for ML_CONNECTED signal (Moonlight handles internal fullscreen)
            // 3. Launch game process on host

            // EN: Request fullscreen on the parent container so Moonlight can expand to the whole screen.
            // FR: Demander le plein écran sur le conteneur parent pour que Moonlight remplisse l'écran.
            try {
                const sv = document.getElementById('streamView');
                if (sv && sv.requestFullscreen) {
                    sv.requestFullscreen().catch(e => console.warn('FS error', e));
                }
            } catch(e) {}

            const t = publicTranslations[currentLang];
            
            // Mark state as waiting
            isWaitingForML = true;
            waitStartTimestamp = new Date().getTime();
            pendingLaunchPath = path;

            // Force streaming view immediately
            const lv = document.getElementById('listView');
            const sv = document.getElementById('streamView');
            const searchContainer = document.getElementById('publicSearchContainer');
            const title = document.getElementById('viewTitle');

            if (!isStreaming) {
                isStreaming = true;
                lv.style.display = 'none';
                searchContainer.style.display = 'none';
                sv.style.display = 'block';
                title.innerText = t.viewTitleStream;

                const overlay = document.getElementById('gameLaunchOverlay');
                const overlayText = document.getElementById('overlayText');
                if (overlay) {
                    overlay.style.display = 'flex';
                    if (overlayText) {
                        overlayText.innerText = (path === '[RETROBAT_UI]') ? t.msgLoadingRb : t.msgConnecting;
                    }
                }

                const token = localStorage.getItem('batrun_token') || '';
                // [BATRUN-MOD]: PASSIVE TARGET EXTRACTION (Robust version)
                // EN: Extract target from relative apiBase if present
                // [BATRUN-HUB]: target is now a machine name alias, not an IP
                let tIp = '';
                if (apiBase && apiBase.includes('target=')) {
                    try {
                        const search = apiBase.split('?')[1];
                        const params = new URLSearchParams(search);
                        tIp = params.get('target') || '';
                    } catch(e) {}
                }
                const streamUrl = `${window.location.protocol}//${window.location.host}/api/moonlight-auth?token=${encodeURIComponent(token)}&hostId=auto&appId=0${tIp ? '&targetIp='+encodeURIComponent(tIp) : ''}`;
                const iframe = document.getElementById('streamIframe');
                batrunStartStream(iframe, streamUrl, () => { setTimeout(() => { iframe.focus(); sv.onclick = () => iframe.focus(); }, 1000); });
                }
            
            closeGameDetails();
        }

        // [BATRUN-FORK] Handle RetroBat Interface UI Launch
        function openRbConfirm() {
            _savedGpIndex = _gpFocusedIndex; // Save current index
            const m = document.getElementById('rbConfirmModal');
            m.style.display = 'flex';
            requestAnimationFrame(() => m.style.opacity = 1);
            setTimeout(() => {
                const focusables = getPubFocusables();
                const btnIdx = focusables.indexOf(document.getElementById('btnOpenRbModal')); 
                const cancelIdx = focusables.indexOf(document.getElementById('btnRbCancel'));
                if (cancelIdx >= 0) gpSetFocus(cancelIdx);
            }, 50);
        }

        function closeRbConfirm() {
            const m = document.getElementById('rbConfirmModal');
            m.style.opacity = 0;
            setTimeout(() => m.style.display = 'none', 300);
            _gpContext = 'main';
            gpSetFocus(_savedGpIndex); // Restore
        }

        function executeRbLaunch() {
            closeRbConfirm();
            isRbStream = true;
            launchGame('[RETROBAT_UI]');
        }

        // [BATRUN-v3]: Stream Retry Modal logic
        function showRetryModal() {
            const t = publicTranslations[currentLang];
            const m = document.getElementById('retryConfirmModal');
            if (!m) return;
            
            document.getElementById('retryModalTitle').innerText = t.retryModalTitle;
            document.getElementById('retryModalDesc').innerText = t.retryModalDesc;
            document.getElementById('btnRetryCancel').innerText = t.retryCancel;
            document.getElementById('btnRetryConfirm').innerText = t.retryConfirm;

            m.style.display = 'flex';
            requestAnimationFrame(() => m.style.opacity = 1);
            
            _savedGpIndex = _gpFocusedIndex;
            setTimeout(() => {
                const focusables = getPubFocusables();
                const confirmIdx = focusables.indexOf(document.getElementById('btnRetryConfirm'));
                if (confirmIdx >= 0) gpSetFocus(confirmIdx);
            }, 50);
        }

        function closeRetryModal() {
            const m = document.getElementById('retryConfirmModal');
            if (m) {
                m.style.opacity = 0;
                setTimeout(() => m.style.display = 'none', 300);
            }
            _gpContext = 'main';
            // Return to list view properly
            isStreaming = false;
            isWaitingForML = false;
            isWaitingForInput = false;
            document.getElementById('listView').style.display = 'flex';
            document.getElementById('publicSearchContainer').style.display = 'flex';
            document.getElementById('streamView').style.display = 'none';
            document.getElementById('viewTitle').innerText = publicTranslations[currentLang].viewTitleIdle;
            gpSetFocus(_savedGpIndex);
        }

        async function executeRetryLaunch() {
            const path = pendingLaunchPath;
            closeRetryModal();
            
            if (!path) {
                console.warn('[BatRun] No pending launch path for retry.');
                return;
            }

            // EN: Force stop any stale session before retrying
            // FR: Forcer l'arrêt de toute session persistante avant de réessayer
            console.log('[BatRun] Executing retry: force stopping previous session first.');
            try {
                const token = localStorage.getItem('batrun_token') || '';
                await fetch('/api/action/force_stop?token=' + encodeURIComponent(token), { method: 'POST' });
            } catch(e) {}

            // Wait a bit for server to clean up
            setTimeout(() => {
                launchGame(path);
            }, 1000);
        }

        // EN: Show game details modal / FR: Afficher la fiche de détail
        let _currentGamePath = null;
        async function showGameDetails(game) {
            // [BATRUN-FORK-v6]: Allow opening game details even when lobby is active or game is in progress.
            // The launch button inside the modal is locked/changed instead of blocking the whole modal.
            // FR: Permettre l'ouverture des détails du jeu même quand un lobby est actif ou un jeu en cours.
            // Le bouton de lancement dans la modale est verrouillé/modifié au lieu de bloquer toute la modale.
            const _lobbyActive = (typeof lobbyPhase !== 'undefined' && lobbyPhase !== 'none');
            const _gameBusy = globalStatus && globalStatus.isGameInProgress && globalStatus.isWebLaunch;
            const _localBusy = globalStatus && globalStatus.isGameInProgress && !globalStatus.isWebLaunch;
            _currentGamePath = game.path;
            const t = publicTranslations[currentLang];
            const modal = document.getElementById('gameModal');
            const img = document.getElementById('mdImage');
            const video = document.getElementById('mdVideo');
            
            document.getElementById('mdTitle').innerText = game.name || '';
            document.getElementById('mdSystem').innerText = game.system || '';
            document.getElementById('mdDesc').innerText = '...';

            // EN: Reset media / FR: Réinitialiser les médias
            img.style.display = 'none'; img.src = '';
            video.style.display = 'none'; video.src = '';

            const fallbackImg = game.thumbnail || game.image || game.fanart || game.marquee;
            if (fallbackImg) {
                img.src = getRelayUrl('/api/es/media?path=' + encodeURIComponent(fallbackImg));
                img.style.display = 'block';
            }

            const anyBusy = globalStatus && globalStatus.isGameInProgress;
            const btn = document.getElementById('btnLaunchModal');
            // [BATRUN-FORK-v6]: Button logic:
            if (_lobbyActive) {
            btn.innerText = '🔒 ' + (t.lobbyActive || 'Lobby en cours');
            btn.style.opacity = '0.5';
            btn.style.cursor = 'not-allowed';
            btn.title = t.lobbyActive || 'Un lobby est en cours. Rejoignez la session.';
            btn.onclick = () => {}; // No action — lobby is active
            } else if (_localBusy) {
            btn.innerText = '⛔ ' + (t.localBusy || 'Busy (Local Game)');
            btn.style.opacity = '0.5';
            btn.style.cursor = 'not-allowed';
            btn.title = t.localBusyHint || 'A game is currently running on the physical machine.';
            btn.onclick = () => {};
            } else if (_gameBusy) {
            const gameName = globalStatus.currentGameName || '';
            btn.innerText = '🔗 ' + t.btnJoinSession;
            btn.style.opacity = '1';
            btn.style.cursor = 'pointer';
            btn.title = t.joinSessionHint + ' ' + gameName;
            btn.onclick = () => joinExistingSession();
            } else {
            btn.innerText = t.btnStart;
            btn.style.opacity = '1';
            btn.style.cursor = 'pointer';
            btn.title = '';
            btn.onclick = () => launchGame(game.path);
            }

            // EN: Show immediately, then fetch rich metadata / FR: Affichage immédiat, puis enrichissement
            _savedGpIndex = _gpFocusedIndex; // Save current scroll position
            modal.style.display = 'flex';
            requestAnimationFrame(() => {
                modal.classList.add('show');
                // EN: Auto-focus LAUNCH button for gamepad / FR: Mettre le focus manette sur DEMARRER
                setTimeout(() => {
                    const focusables = getPubFocusables();
                    const btnLaunchIdx = focusables.indexOf(document.getElementById('btnLaunchModal'));
                    if (btnLaunchIdx >= 0) gpSetFocus(btnLaunchIdx);
                }, 50);
            });

            try {
                const metaRes = await fetch(getRelayUrl('/api/es/metadata?system=' + encodeURIComponent(game.system) + '&path=' + encodeURIComponent(game.path)));
                if (metaRes.ok) {
                    const meta = await metaRes.json();
                    if (meta.desc) document.getElementById('mdDesc').innerText = meta.desc;
                    else document.getElementById('mdDesc').innerText = t.noGames || 'Aucune description.';

                    // [BATRUN-FIX]: Capture current focus before modifying DOM so we can restore it.
                    // The video element dynamically showing up shifts the focusable array indices.
                    let currentFocusId = null;
                    if (_gpContext === 'modal' && _gpFocusedIndex >= 0) {
                        const oldFocusables = getPubFocusables();
                        if (oldFocusables[_gpFocusedIndex]) currentFocusId = oldFocusables[_gpFocusedIndex].id;
                    }

                    if (meta.video) {
                        video.src = getRelayUrl('/api/es/media?path=' + encodeURIComponent(meta.video));
                        video.style.display = 'block';
                        img.style.display = 'none';
                    }

                    if (currentFocusId) {
                        const newFocusables = getPubFocusables();
                        const newIdx = newFocusables.findIndex(el => el.id === currentFocusId);
                        if (newIdx >= 0) gpSetFocus(newIdx);
                    }
                }
            } catch(e) {
                document.getElementById('mdDesc').innerText = '...';
            }
        }

        function closeGameDetails() {
            const modal = document.getElementById('gameModal');
            modal.classList.remove('show');
            setTimeout(() => { modal.style.display = 'none'; }, 300);
            const video = document.getElementById('mdVideo');
            video.src = ''; // EN: Stop video / FR: Arrêter la vidéo
            _gpContext = 'main';
            gpSetFocus(_savedGpIndex); // Restore focus to the game in list
        }

        function requestStreamFullscreen() {
            const sv = document.getElementById('streamView');
            if (sv && sv.requestFullscreen) {
                sv.requestFullscreen().then(async () => {
                    syncFullscreenUI();
                    
                    // [BATRUN-FORK] Lock Escape key to prevent accidental browser exit
                    if (navigator.keyboard && navigator.keyboard.lock) {
                        try {
                            await navigator.keyboard.lock(['Escape']);
                            console.log('[BatRun] Keyboard locked: Escape');
                        } catch(e) { console.warn('[BatRun] Keyboard lock failed', e); }
                    }
                }).catch(e => console.warn('[BatRun] Fullscreen error:', e));
            }
        }

        // EN: Hide search bar AND header on scroll down – show on scroll up
        // FR: Masquer la barre de recherche ET le header au scroll bas, afficher au scroll haut
        let _lastScrollTop = 0;
        document.getElementById('publicGamesList').addEventListener('scroll', function() {
            const st = this.scrollTop;
            const sc = document.getElementById('publicSearchContainer');
            const hd = document.getElementById('publicHeader');
            
            const kb = document.getElementById('searchVirtualKeyboard');
            const isKbOpen = kb && kb.style.display === 'flex';
            
            if (st > _lastScrollTop && st > 50 && !isKbOpen) {
                // Scroll down
                sc.classList.add('search-hidden');
                hd.classList.add('search-hidden');
            } else if (st < _lastScrollTop) {
                // Scroll up
                sc.classList.remove('search-hidden');
                // Don't necessarily show header on every micro scroll up if we want to save space
                if (st < 100) hd.classList.remove('search-hidden');
            }
            _lastScrollTop = st;
        });

        function showControls() {
            document.getElementById('publicSearchContainer').classList.remove('search-hidden');
            document.getElementById('publicHeader').classList.remove('search-hidden');
        }

        // EN: Gamepad navigation / FR: Navigation manette
        let _gpFocusedIndex = -1;
        let _gpLastButtons = {};
        let _gpActive = false;

        // EN: Deactivate Gamepad mode on mouse/touch interaction to prevent virtual keyboard
        // FR: Désactiver le mode manette lors d'interaction souris/tactile pour éviter le clavier virtuel
        window.addEventListener('mousedown', () => { 
            _gpActive = false; 
            document.querySelectorAll('input').forEach(i => i.removeAttribute('inputmode'));
        });
        window.addEventListener('touchstart', () => { 
            _gpActive = false; 
            document.querySelectorAll('input').forEach(i => i.removeAttribute('inputmode'));
        });

        // EN: Context enum for gamepad navigation / FR: Contexte actif pour la navigation manette
        // Contexts: 'main' | 'modal' | 'system' | 'keyboard' | 'auth'
        let _gpContext = 'main';
        let _vkFocusedIndex = 0;
        let _sysFocusedIndex = 0;
        let _activeInput = null; // Track which input field has focus for the virtual keyboard
        let _activeKb = null;

        function getPubFocusables() {
            const container = document.getElementById('gameContainer');
            if (container && container.style.display !== 'none') {
                const modal = document.getElementById('gameModal');
                const sysModal = document.getElementById('systemSelectModal');
                const genreModal = document.getElementById('genreSelectModal');
                const sortModal = document.getElementById('sortSelectModal');
                const rbModal = document.getElementById('rbConfirmModal');
                
                // [BATRUN] Session close confirmation modal — highest priority (z-index: 100001)
                // FR: Modal de confirmation de fermeture de session — priorité maximale
                const emergencyModal = document.getElementById('emergencyConfirmModal');
                if (emergencyModal && emergencyModal.style.display === 'flex') {
                    _gpContext = 'emergency';
                    return [document.getElementById('btnEmergencyCancel'), document.getElementById('btnEmergencyConfirm')];
                }
    
                if (rbModal && rbModal.style.display === 'flex') {
                    _gpContext = 'rbmodal';
                    return [document.getElementById('btnRbCancel'), document.getElementById('btnRbConfirm')];
                }

                const retryModal = document.getElementById('retryConfirmModal');
                if (retryModal && retryModal.style.display === 'flex') {
                    _gpContext = 'retrymodal';
                    return [document.getElementById('btnRetryCancel'), document.getElementById('btnRetryConfirm')];
                }
                
                if (sysModal && sysModal.style.display === 'flex') {
                    _gpContext = 'system';
                    return [...sysModal.querySelectorAll('.system-option')];
                }
                if (genreModal && genreModal.style.display === 'flex') {
                    _gpContext = 'system';
                    return [...genreModal.querySelectorAll('.system-option')];
                }
                if (sortModal && sortModal.style.display === 'flex') {
                    _gpContext = 'system';
                    return [...sortModal.querySelectorAll('.system-option')];
                }
                if (_activeKb && _activeKb.style.display === 'flex') {
                    _gpContext = 'keyboard';
                    return [..._activeKb.querySelectorAll('.vk-key')];
                }
                if (modal && modal.classList.contains('show')) {
                    _gpContext = 'modal';
                    const items = [];
                    const btnClose = modal.querySelector('.modal-close');
                    const mdVideo = document.getElementById('mdVideo');
                    const mdDesc = document.getElementById('mdDesc');
                    const btnLaunch = document.getElementById('btnLaunchModal');
                    if (btnClose) items.push(btnClose);
                    if (mdVideo && mdVideo.style.display !== 'none') items.push(mdVideo);
                    if (mdDesc) items.push(mdDesc);
                    if (btnLaunch) items.push(btnLaunch);
                    return items;
                }
                _gpContext = 'main';
                const mainEls = [...document.querySelectorAll('#publicGamesList .game-item, #publicGamesList .emergency-rb-btn, #customSystemSelect, #customGenreSelect, #customSortSelect, #gameSearchInput, #btnOpenRbModal, #btnHistoryToggle, #btnFullscreenPage, #logoutBtn')];
                const joinBtn = document.getElementById('lobbyJoinBannerBtn');
                if (joinBtn && joinBtn.offsetParent !== null) {
                    mainEls.unshift(joinBtn); // Make it the first reachable item if visible
                }
                return mainEls;
            } else {
                // Auth page focusables
                // [BATRUN-FIX]: Check for active virtual keyboard FIRST (same as game container branch)
                // FR: Vérifier le clavier virtuel actif EN PREMIER (comme la branche game container)
                // Without this, getPubFocusables() returns auth elements and overrides _gpContext='auth',
                // breaking gamepad navigation on the virtual keyboard keys.
                if (_activeKb && _activeKb.style.display === 'flex') {
                    _gpContext = 'keyboard';
                    return [..._activeKb.querySelectorAll('.vk-key')];
                }
                const authC = document.getElementById('authContainer');
                if (!authC) return [];
                const res = [];
                // EN: Add nodes FIRST in the cycle / FR: Ajouter les bornes EN PREMIER dans le cycle
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
        }

        function gpSetFocus(idx) {
            const els = getPubFocusables();
            if (!els.length) return;

            // [BATRUN] Handle video mute when focus leaves
            const oldEl = els[_gpFocusedIndex];
            if (oldEl && oldEl.id === 'mdVideo' && _gpContext === 'modal') {
                oldEl.muted = true;
            }

            els.forEach(e => e.classList.remove('focused'));
            _gpFocusedIndex = Math.max(0, Math.min(idx, els.length - 1));
            const el = els[_gpFocusedIndex];
            el.classList.add('focused');
            el.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
            
            // [BATRUN] Track focus to send keyboard inputs to the correct specific field (user vs pass)
            if (el.tagName === 'INPUT') {
                _activeInput = el;
                // [BATRUN-MOD]: Prevent native keyboard on mobile when using gamepad
                // FR: Empêcher le clavier natif sur mobile lors de l'utilisation de la manette
                if (_gpActive) el.setAttribute('inputmode', 'none');
            }

            // [BATRUN] Unmute video when focused
            if (el.id === 'mdVideo' && _gpContext === 'modal') {
                el.muted = false;
            }
        }

        function gpNavigate(dir) {
            let els = getPubFocusables();
            if (!els.length) return;
            if (_gpFocusedIndex < 0) { gpSetFocus(0); return; }
            // [BATRUN-FIX]: Count actual rendered grid columns instead of parsing CSS string.
            // FR: Compter les colonnes réelles rendues au lieu d'analyser la chaîne CSS.
            // CSS repeat(auto-fill, minmax(180px,1fr)) cannot be parsed by split(' ')[0].
            // Instead, count how many game-items share the same top offset as the first item.
            const grid = document.getElementById('publicGamesList');
            const gameItems = grid ? grid.querySelectorAll('.game-item') : [];
            let cols = 1;
            if (gameItems.length >= 2) {
                const firstTop = gameItems[0].getBoundingClientRect().top;
                for (let i = 1; i < gameItems.length; i++) {
                    if (Math.abs(gameItems[i].getBoundingClientRect().top - firstTop) < 5) {
                        cols++;
                    } else {
                        break; // reached next row
                    }
                }
            }
            cols = Math.max(1, cols);

            // [BATRUN-FIX]: Find the first game-item index in the focusables list.
            // FR: Trouver l'index du premier game-item dans la liste des focusables.
            // Non-grid items (search, system select, RB button) sit before game items.
            // Up/down must handle the boundary between header and grid.
            let firstGameIdx = -1;
            for (let i = 0; i < els.length; i++) {
                if (els[i].classList && els[i].classList.contains('game-item')) {
                    firstGameIdx = i;
                    break;
                }
            }

            let next = _gpFocusedIndex;
            const curEl = els[_gpFocusedIndex];
            const isInGrid = curEl && curEl.classList && curEl.classList.contains('game-item');

            if (dir === 'right') {
                next++;
            } else if (dir === 'left') {
                next--;
            } else if (dir === 'down') {
                if (!isInGrid && firstGameIdx >= 0) {
                    // EN: Down from header item → jump to first game item
                    // FR: Bas depuis un élément d'en-tête → aller au premier jeu
                    next = firstGameIdx;
                } else if (isInGrid) {
                    next += cols;
                } else {
                    next++;
                }
            } else if (dir === 'up') {
                if (isInGrid) {
                    const gridIdx = _gpFocusedIndex - firstGameIdx;
                    // EN: If in first grid row, go up to last header item
                    // FR: Si dans la première ligne de la grille, aller au dernier élément d'en-tête
                    if (gridIdx < cols) {
                        next = firstGameIdx > 0 ? firstGameIdx - 1 : 0;
                    } else {
                        next -= cols;
                    }
                } else {
                    next--;
                }
            }

            // [BATRUN-FORK] Infinite Scroll support for gamepad: load next chunk if we reach the end
            if (next >= els.length && _gpContext === 'main') {
                if ((renderPage + 1) * RENDER_CHUNK < currentGamesList.length) {
                    renderPage++;
                    renderGamesChunk();
                    els = getPubFocusables(); // Refresh elements list after DOM update
                }
            }

            next = Math.max(0, Math.min(next, els.length - 1));
            gpSetFocus(next);
        }

        let _gpScrollMode = false;
        (function pollGamepads() {
            // [BATRUN-FORK]: Skip parent gamepad navigation if we are currently streaming a game
            // BUT: Allow navigation if the emergency stop confirmation modal is visible
            // FR: Ignorer la navigation gamepad si on stream un jeu, SAUF si la modal de fermeture est visible
            if (typeof isStreaming !== 'undefined' && isStreaming && !_emergencyConfirmShown) {
            	requestAnimationFrame(pollGamepads);
            	return;
            }
            // EN: Skip if waitingForInput (handled by ML_CONNECTED flow) / FR: Ignorer si en attente d'input
            if (typeof isWaitingForInput !== 'undefined' && isWaitingForInput && !_emergencyConfirmShown) {
            	requestAnimationFrame(pollGamepads);
            	return;
            }
            // [BATRUN-FORK]: Skip if in lobby phase (handled by lobbyPollGamepads)
            // FR: Ignorer si en phase lobby (géré par lobbyPollGamepads)
            if (typeof lobbyPhase !== 'undefined' && lobbyPhase !== 'none' && lobbyPhase !== 'launching' && !_emergencyConfirmShown) {
            	requestAnimationFrame(pollGamepads);
            	return;
            }

            const gps = navigator.getGamepads ? navigator.getGamepads() : [];
            for (const gp of gps) {
                if (!gp) continue;
                const prev = _gpLastButtons[gp.index] || {};
                const pressed = (btn, i) => btn.pressed && !prev[i];

                const ax0 = gp.axes[0] || 0, ax1 = gp.axes[1] || 0;
                const now = performance.now();

                // [BATRUN] Auto-fullscreen on first gamepad interaction if enabled
                if (!_fsAutoInit && localStorage.getItem('batrun_fullscreen') === 'true') {
                    if (gp.buttons.some(b => b.pressed) || Math.abs(ax0) > 0.5 || Math.abs(ax1) > 0.5) {
                        _fsAutoInit = true;
                        if (!document.fullscreenElement) {
                            document.documentElement.requestFullscreen().catch(err => console.log(err));
                        }
                    }
                }

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

                // EN: Context-aware navigation / FR: Navigation selon le contexte actif
                const ctxEls = getPubFocusables();

                if (dRight || dLeft || dDown || dUp) {
                    if (typeof autoForceFullscreen === 'function') autoForceFullscreen();
                    _gpActive = true;
                    if (_gpContext === 'keyboard') {
                        // EN: Keyboard: navigate among keys / FR: Clavier : naviguer parmi les touches
                        const cols = 10; // approx keys per row
                        let ni = _vkFocusedIndex;
                        if (dRight) ni++;
                        else if (dLeft) ni--;
                        else if (dDown) ni += cols;
                        else if (dUp) ni -= cols;
                        ni = Math.max(0, Math.min(ni, ctxEls.length - 1));
                        _vkFocusedIndex = ni;
                        ctxEls.forEach(k => k.classList.remove('focused'));
                        ctxEls[ni]?.classList.add('focused');
                        ctxEls[ni]?.scrollIntoView({block:'nearest'});
                    } else if (_gpContext === 'system') {
                    	// [BATRUN-FIX]: Count actual rendered grid columns instead of guessing by width.
                    	// FR: Compter les colonnes réelles rendues au lieu de deviner par la largeur.
                    	let grid = document.getElementById('systemSelectGrid');
                        if (document.getElementById('genreSelectModal') && document.getElementById('genreSelectModal').style.display === 'flex') {
                            grid = document.getElementById('genreSelectGrid');
                        } else if (document.getElementById('sortSelectModal') && document.getElementById('sortSelectModal').style.display === 'flex') {
                            grid = document.getElementById('sortSelectGrid');
                        }
                    	const sysItems = grid ? grid.querySelectorAll('.system-option') : [];
                    	let cols = 1;
                    	if (sysItems.length >= 2) {
                    	    const firstTop = sysItems[0].getBoundingClientRect().top;
                    	    for (let i = 1; i < sysItems.length; i++) {
                    	        if (Math.abs(sysItems[i].getBoundingClientRect().top - firstTop) < 5) {
                    	            cols++;
                    	        } else {
                    	            break;
                    	        }
                    	    }
                    	}
                    	cols = Math.max(1, cols);
                    	let ni = _sysFocusedIndex;
                    	if (dRight) ni++;
                    	else if (dLeft) ni--;
                    	else if (dDown) ni += cols;
                    	else if (dUp) ni -= cols;
                    	ni = Math.max(0, Math.min(ni, ctxEls.length - 1));
                    	_sysFocusedIndex = ni;
                    	ctxEls.forEach(k => k.classList.remove('focused'));
                    	ctxEls[ni]?.classList.add('focused');
                    	ctxEls[ni]?.scrollIntoView({block:'nearest', behavior:'smooth'});
                    } else if (_gpContext === 'emergency' || _gpContext === 'retrymodal') {
                    	// EN: Navigation for simple 2-button modals (left/right)
                    	// FR: Navigation pour les modales simples à 2 boutons (gauche/droite)
                    	if (dRight) { gpSetFocus(Math.min(_gpFocusedIndex + 1, ctxEls.length - 1)); }
                    	else if (dLeft) { gpSetFocus(Math.max(_gpFocusedIndex - 1, 0)); }
                    } else {
                    	// EN: Main / modal navigation / FR: Navigation principale / modal
                        const isModalDesc = (_gpContext === 'modal' && _gpFocusedIndex >= 0 && ctxEls[_gpFocusedIndex] && ctxEls[_gpFocusedIndex].id === 'mdDesc');
                        if (isModalDesc && _gpScrollMode) {
                            if (dDown || dUp) {
                                ctxEls[_gpFocusedIndex].scrollTop += (dDown ? 40 : -40);
                            }
                        } else {
                    	    if (dRight) gpNavigate('right');
                    	    else if (dLeft) gpNavigate('left');
                    	    else if (dDown) gpNavigate('down');
                    	    else if (dUp) gpNavigate('up');
                        }
                    }
                }

                // EN: A (button 0) = confirm/click / FR: A = confirmer/cliquer
                if (pressed(gp.buttons[0], 0)) {
                    if (typeof autoForceFullscreen === 'function') autoForceFullscreen();
                    if (_gpContext === 'keyboard') {
                        ctxEls[_vkFocusedIndex]?.click();
                    } else if (_gpContext === 'system') {
                        ctxEls[_sysFocusedIndex]?.click();
                    } else if (_gpContext === 'emergency' || _gpContext === 'retrymodal') {
                        // [BATRUN] Emergency/Retry modal: A = click focused button (Cancel or Confirm)
                        const els = getPubFocusables();
                        if (_gpFocusedIndex >= 0 && els[_gpFocusedIndex]) els[_gpFocusedIndex].click();
                        else if (els.length) gpSetFocus(0);
                    } else {
                        const els = getPubFocusables();
                        if (_gpFocusedIndex >= 0 && els[_gpFocusedIndex]) {
                            const el = els[_gpFocusedIndex];
                            if (_gpContext === 'modal' && el.id === 'mdDesc') {
                                _gpScrollMode = true;
                                el.style.outlineColor = 'var(--success)';
                            } else {
                                el.click();
                            }
                        }
                        else if (els.length) gpSetFocus(0);
                    }
                }
    
                // EN: B (button 1) = back/close / FR: B = retour/fermer
                if (pressed(gp.buttons[1], 1)) {
                    if (typeof autoForceFullscreen === 'function') autoForceFullscreen();
                    if (_gpContext === 'keyboard') {
                        closeVirtualKeyboard();
                    } else if (_gpContext === 'system') {
                        if (document.getElementById('genreSelectModal') && document.getElementById('genreSelectModal').style.display === 'flex') {
                            closeGenreModal();
                        } else if (document.getElementById('sortSelectModal') && document.getElementById('sortSelectModal').style.display === 'flex') {
                            closeSortModal();
                        } else {
                            closeSystemModal();
                        }
                    } else if (_gpContext === 'emergency') {
                        cancelEmergencyStop();
                    } else if (_gpContext === 'retrymodal') {
                        closeRetryModal();
                    } else {
                        const modal = document.getElementById('gameModal');
                        if (modal && modal.classList.contains('show')) {
                            if (_gpScrollMode) {
                                _gpScrollMode = false;
                                const mdDesc = document.getElementById('mdDesc');
                                if (mdDesc) mdDesc.style.outlineColor = '';
                            } else {
                                closeGameDetails();
                            }
                        }
                    }
                }

                // EN: Y (button 3) = open system selector / FR: Y = ouvrir le sélecteur de système
                if (pressed(gp.buttons[3], 3) && _gpContext === 'main') {
                    openSystemModal();
                }

                // EN: X (button 2) or Start (button 9) = open virtual keyboard / FR: X ou Start = ouvrir le clavier virtuel
                if (pressed(gp.buttons[2], 2) || pressed(gp.buttons[9], 9)) {
                    // [BATRUN-FIX]: Block VK when auth page is visible BUT there are no login fields displayed (Guest mode).
                    // Must NOT use 'return' here — it would exit pollGamepads() and kill the loop.
                    const _authPage = document.getElementById('authPage');
                    const _loginFields = document.getElementById('loginFields');
                    
                    // FR: Bloquer le clavier uniquement si on est sur la page d'auth et que les champs de login sont cachés.
                    const _vkBlocked = _authPage && _authPage.style.display !== 'none' && (!_loginFields || _loginFields.style.display === 'none');

                    if (!_vkBlocked && (_gpContext === 'main' || _gpContext === 'auth')) {
                        // EN: Ensure we have an active input if in auth context
                        if (_gpContext === 'main') {
                            _activeInput = document.getElementById('gameSearchInput');
                            showControls();
                            document.getElementById('publicGamesList').scrollTop = 0;
                            if (_activeInput) _activeInput.focus();
                        } else if (_gpContext === 'auth' && !_activeInput) {
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

        // ====================================================================
        // EN: VIRTUAL KEYBOARD - build AZERTY or QWERTY based on language
        // FR: CLAVIER VIRTUEL - construction AZERTY ou QWERTY selon la langue
        // ====================================================================
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
        let _vkShift = false;


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
        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape') {
                if (_gpContext === 'keyboard') closeVirtualKeyboard();
                else if (_gpContext === 'system') closeSystemModal();
                else closeGameDetails();
            }
        });

        // EN: Listen to input focus to set _activeInput and optionally open keyboard
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
            // Native keyboard pops up properly. Do not force VK on mouse/touch clicks.
        });

        async function stopLaunch() {
            const token = localStorage.getItem('batrun_token');
            isRbStream = false; // Disable special retrobat stream flag
            isStreaming = false; // EN: Essential for next launch / FR: Crucial pour permettre le lancement suivant
            isWaitingForML = false;
            isWaitingForInput = false;
            pendingLaunchPath = null;
            // [BATRUN-FORK]: Reset lobby state on stop
            // FR: Réinitialiser l'état du lobby à l'arrêt
            lobbyPhase = 'none';
            lobbyStopPolling();
            _lobbyPrevButtons = {};
            _lobbyActionCooldown = 0;
            _lobbyPollDebugLogged = false;
            _isLobbyP1 = false;
            const lobbyOverlay = document.getElementById('lobbyOverlay');
            if (lobbyOverlay) lobbyOverlay.style.display = 'none';
            const banner = document.getElementById('lobbyBanner');
            if (banner) banner.style.display = 'none';
            // EN: Unblock gamepad input in Moonlight iframe AND cleanly stop the old stream
            // FR: Débloquer les inputs manette dans l'iframe Moonlight ET arrêter proprement l'ancien stream
            const _stopIframe = document.getElementById('streamIframe');
            if (_stopIframe && _stopIframe.contentWindow) {
            try { _stopIframe.contentWindow.postMessage({type: 'BATRUN_LOBBY_INPUT_BLOCK', blocked: false}, '*'); } catch(e) {}
            // [BATRUN-FORK-v8]: Send BATRUN_STOP_STREAM to cleanly disconnect the virtual controller
            // on the host. Without this, the virtual Xbox controller persists for several seconds
            // after the stream ends, causing a second virtual controller to be created on next launch.
            // FR: Envoyer BATRUN_STOP_STREAM pour déconnecter proprement la manette virtuelle
            // sur l'hôte. Sans cela, la manette Xbox virtuelle persiste plusieurs secondes
            // après la fin du stream, causant la création d'une deuxième manette au prochain lancement.
            try { _stopIframe.contentWindow.postMessage({type: 'BATRUN_STOP_STREAM'}, '*'); } catch(e) {}
            }
            // [BATRUN-FORK-v9]: Send /cancel to Sunshine via the BatRun server to immediately
            // disconnect the virtual controller on the host. This is the same mechanism that
            // Moonlight Desktop uses: GET https://<host>:47984/cancel?uniqueid=...&uuid=...
            // FR: Envoyer /cancel à Sunshine via le serveur BatRun pour déconnecter immédiatement
            // la manette virtuelle sur l'hôte. C'est le même mécanisme que le client Moonlight Desktop.
            try {
            fetch(getRelayUrl('/api/public/sunshine-cancel'), {
            method: 'POST',
            headers: {'Content-Type': 'application/json'},
            body: JSON.stringify({ token: token })
            }).catch(() => {});
            } catch(e) {}
            // EN: Blank iframe after 3s — gives Moonlight time to process BATRUN_STOP_STREAM
            //     and cleanly release the virtual controller before the iframe context is destroyed.
            // FR: Vider iframe après 3s — donne à Moonlight le temps de traiter BATRUN_STOP_STREAM
            //     et libérer proprement la manette virtuelle avant que le contexte de l'iframe soit détruit.
            setTimeout(() => { try { if (_stopIframe) _stopIframe.src = 'about:blank'; } catch(e) {} }, 3000);
            _batrunStreamCooldownUntil = Date.now() + 2000;

            // EN: Notify server that this session left the lobby
            // FR: Notifier le serveur que cette session a quitté le lobby
            try {
                fetch(getRelayUrl('/api/public/lobby/leave'), {
                    method: 'POST',
                    headers: {'Content-Type': 'application/json'},
                    body: JSON.stringify({ token: token, sessionId: _lobbySessionId })
                });
            } catch(e) {}
            
            try {
                const res = await fetch(getRelayUrl('/api/public/stop'), {
                    method: 'POST',
                    headers: {'Content-Type': 'application/json'},
                    body: JSON.stringify({ token: token })
                });
                if(res.ok) {
                    checkStatus();
                }
            } catch (e) {
                console.error('Stop error', e);
            }
            }

            // ====================================================================
            // [BATRUN] Emergency Stop — Select+Start 5s long press detection + confirmation
            // FR: Arrêt d'urgence — Détection pression longue Select+Start 5s + confirmation
            // ====================================================================

            // EN: Detect Select+Start long press from browser gamepad API (for web stream sessions)
            // FR: Détecter la pression longue Select+Start depuis l'API gamepad du navigateur (sessions stream web)
            function emergencyPollGamepads() {
                // EN: Only detect when streaming (in stream view) OR when BATRUN_FORCE_STOP can come from iframe
                // FR: Détecter uniquement quand on est en streaming (dans la vue stream)
                // [BATRUN-v3]: Also detect when isRbStream (RetroBat interface stream)
                // FR: Détecter aussi quand isRbStream (stream de l'interface RetroBat)
                if (!isStreaming && !isWaitingForML && !isRbStream) {
                    if (_emergencyComboActive) emergencyResetHold();
                    requestAnimationFrame(emergencyPollGamepads);
                    return;
                }
    
                // EN: Skip if confirmation modal is already shown
                // FR: Ignorer si la modal de confirmation est déjà affichée
                if (_emergencyConfirmShown) {
                    requestAnimationFrame(emergencyPollGamepads);
                    return;
                }

                const gps = navigator.getGamepads ? navigator.getGamepads() : [];
                let anyComboActive = false;

                for (let i = 0; i < gps.length; i++) {
                    const gp = gps[i];
                    if (!gp) continue;

                    // EN: Select = button 8 (Back), Start = button 9
                    // FR: Select = bouton 8 (Back), Start = bouton 9
                    const isSelectDown = gp.buttons[8] && gp.buttons[8].pressed;
                    const isStartDown = gp.buttons[9] && gp.buttons[9].pressed;

                    if (isSelectDown && isStartDown) {
                        anyComboActive = true;

                        if (!_emergencyComboActive) {
                            // EN: First frame both buttons are pressed — START the hold timer
                            // FR: Première frame où les deux boutons sont pressés — DÉMARRER le timer
                            _emergencyComboActive = true;
                            _emergencyHoldStart = Date.now();
                            console.log('[EmergencyStop] Select+Start hold STARTED (waiting 800ms before blocking)');
                        }

                        const elapsed = Date.now() - _emergencyHoldStart;

                        // EN: Show overlay after delay (so quick presses don't flash it)
                        // FR: Afficher l'overlay après un délai (les pressions rapides ne le flashent pas)
                        if (elapsed >= EMERGENCY_OVERLAY_DELAY) {
                            const overlay = document.getElementById('emergencyStopOverlay');
                            if (overlay && overlay.style.display !== 'flex') {
                                const t = publicTranslations[currentLang];
                                const msgEl = document.getElementById('emergencyMsg');
                                if (msgEl) msgEl.innerText = t.emergencyHoldMsg || 'SELECT + START = CLOSE SESSION';
                                overlay.style.display = 'flex';
                                overlay.style.pointerEvents = 'none';

                                // [BATRUN-FORK-v11]: Block input ONLY when the emergency overlay appears.
                                // This allows short presses (Select+Start) to pass through to the host.
                                // FR: Bloquer les inputs UNIQUEMENT quand l'overlay d'urgence apparaît.
                                // Cela permet aux appuis courts (Select+Start) de passer vers l'hôte.
                                const _emIframe = document.getElementById('streamIframe');
                                if (_emIframe && _emIframe.contentWindow) {
                                    try { _emIframe.contentWindow.postMessage({type: 'BATRUN_LOBBY_INPUT_BLOCK', blocked: true}, '*'); } catch(e) {}
                                }
                            }
                        }

                        // EN: Update progress ring
                        // FR: Mettre à jour l'anneau de progression
                        if (elapsed >= EMERGENCY_OVERLAY_DELAY) {
                            const progress = Math.min(1, (elapsed - EMERGENCY_OVERLAY_DELAY) / (EMERGENCY_HOLD_DURATION - EMERGENCY_OVERLAY_DELAY));
                            const circumference = 2 * Math.PI * 70; // r=70
                            const offset = circumference * (1 - progress);
                            const circle = document.getElementById('emergencyCircle');
                            if (circle) {
                                circle.style.strokeDasharray = circumference;
                                circle.style.strokeDashoffset = offset;
                            }
                            const secEl = document.getElementById('emergencySec');
                            if (secEl) {
                                const remaining = Math.max(0, Math.ceil((EMERGENCY_HOLD_DURATION - elapsed) / 1000));
                                secEl.innerText = remaining + 's';
                            }
                        }

                        // EN: 5 seconds reached — show confirmation modal
                        // FR: 5 secondes atteintes — afficher la modal de confirmation
                        if (elapsed >= EMERGENCY_HOLD_DURATION) {
                            console.log('[EmergencyStop] 5s hold reached! Showing confirmation...');
                            _emergencyComboActive = false; // Reset hold state
                            emergencyShowConfirm();
                        }

                        break; // EN: One controller is enough / FR: Une manette suffit
                    }
                }

                // EN: If no controller has both buttons pressed, reset the hold timer
                // FR: Si aucune manette n'a les deux boutons pressés, réinitialiser le timer
                if (!anyComboActive && _emergencyComboActive) {
                    emergencyResetHold();
                }

                requestAnimationFrame(emergencyPollGamepads);
            }
            requestAnimationFrame(emergencyPollGamepads);

            function emergencyResetHold() {
                const elapsed = _emergencyComboActive ? (Date.now() - _emergencyHoldStart) : 0;
                _emergencyComboActive = false;
                _emergencyHoldStart = 0;
                // EN: Hide overlay with small delay to avoid flickering on brief releases
                // FR: Cacher l'overlay avec un petit délai pour éviter le scintillement sur les relâches brèves
                setTimeout(() => {
                    if (!_emergencyComboActive) {
                        const overlay = document.getElementById('emergencyStopOverlay');
                        if (overlay) overlay.style.display = 'none';
                        const circle = document.getElementById('emergencyCircle');
                        if (circle) {
                            const circumference = 2 * Math.PI * 70;
                            circle.style.strokeDashoffset = circumference;
                        }
                    }
                }, 200);
                if (elapsed > 0) {
                    console.log('[EmergencyStop] Select+Start released after ' + (elapsed/1000).toFixed(1) + 's — timer RESET');
                    // EN: Unblock input if we are not in confirmation modal and not in lobby/preparation
                    // FR: Débloquer les inputs si on n'est pas dans la modal de confirmation ni en lobby/préparation
                    // EN: Unblock input if we are not in confirmation modal and not in lobby/preparation/launching
                    // FR: Débloquer les inputs si on n'est pas dans la modal de confirmation ni en lobby/préparation/lancement
                    if (!_emergencyConfirmShown && (typeof lobbyPhase === 'undefined' || lobbyPhase === 'none' || lobbyPhase === 'launching') && !isWaitingForInput) {
                        const _emIframe = document.getElementById('streamIframe');
                        if (_emIframe && _emIframe.contentWindow) {
                            try { _emIframe.contentWindow.postMessage({type: 'BATRUN_LOBBY_INPUT_BLOCK', blocked: false}, '*'); } catch(e) {}
                        }
                    }
                }
            }

            function emergencyShowConfirm() {
                _emergencyConfirmShown = true;
                const t = publicTranslations[currentLang];
                const overlay = document.getElementById('emergencyStopOverlay');
                if (overlay) overlay.style.display = 'none';

                const modal = document.getElementById('emergencyConfirmModal');
                const title = document.getElementById('emergencyConfirmTitle');
                const desc = document.getElementById('emergencyConfirmDesc');
                const btnCancel = document.getElementById('btnEmergencyCancel');
                const btnConfirm = document.getElementById('btnEmergencyConfirm');

                if (title) title.innerText = t.emergencyConfirmTitle || 'CLOSE SESSION';
                if (desc) desc.innerText = t.emergencyConfirmDesc || 'Force close the current session?';
                if (btnCancel) btnCancel.innerText = t.emergencyCancel || 'Cancel';
                if (btnConfirm) btnConfirm.innerText = t.emergencyConfirm || 'CONFIRM';

                if (modal) {
                    modal.style.display = 'flex';
                    requestAnimationFrame(() => {
                        modal.style.opacity = 1;
                        // [BATRUN] Auto-focus gamepad on Cancel button (safest default)
                        // FR: Auto-focus manette sur le bouton Annuler (défaut le plus sûr)
                        _savedGpIndex = _gpFocusedIndex;
                        setTimeout(() => {
                            const focusables = getPubFocusables();
                            const cancelIdx = focusables.indexOf(document.getElementById('btnEmergencyCancel'));
                            if (cancelIdx >= 0) gpSetFocus(cancelIdx);
                            else if (focusables.length) gpSetFocus(0);
                        }, 50);
                    });
                }
                }

            function cancelEmergencyStop() {
                _emergencyConfirmShown = false;
                const modal = document.getElementById('emergencyConfirmModal');
                if (modal) {
                    modal.style.opacity = 0;
                    setTimeout(() => modal.style.display = 'none', 300);
                }
                // [BATRUN] Restore gamepad focus to previous position
                // FR: Restaurer le focus manette à la position précédente
                _gpContext = isStreaming ? 'main' : 'main';
                gpSetFocus(_savedGpIndex);

                // EN: Unblock input on cancel (unless in lobby/preparation)
                // FR: Débloquer les inputs à l'annulation (sauf si en lobby/préparation)
                if ((typeof lobbyPhase === 'undefined' || lobbyPhase === 'none' || lobbyPhase === 'launching') && !isWaitingForInput) {
                    const _emIframe = document.getElementById('streamIframe');
                    if (_emIframe && _emIframe.contentWindow) {
                        try { _emIframe.contentWindow.postMessage({type: 'BATRUN_LOBBY_INPUT_BLOCK', blocked: false}, '*'); } catch(e) {}
                    }
                }
            }

            async function confirmEmergencyStop() {
                _emergencyConfirmShown = false;
                const token = localStorage.getItem('batrun_token');
                const t = publicTranslations[currentLang];

                const modal = document.getElementById('emergencyConfirmModal');
                if (modal) {
                    modal.style.opacity = 0;
                    setTimeout(() => modal.style.display = 'none', 300);
                }

                console.log('[EmergencyStop] CONFIRMED! Sending force-stop-session to server...');

                try {
                    const res = await fetch(getRelayUrl('/api/public/force-stop-session'), {
                        method: 'POST',
                        headers: {'Content-Type': 'application/json'},
                        body: JSON.stringify({ token: token })
                    });
                    if (res.ok) {
                    	console.log('[EmergencyStop] Force-stop-session accepted by server.');
                    	// EN: Update local force-stop timestamp so checkStatus() doesn't re-trigger for P1
                    	// FR: Mettre à jour le timestamp d'arrêt forcé local pour que checkStatus() ne se redéclenche pas pour P1
                    	localForceStopTime = Date.now();
                    	// EN: Reset local streaming state and go back to game list
                    	// FR: Réinitialiser l'état de streaming local et retourner à la liste des jeux
                    	isStreaming = false;
                        isWaitingForML = false;
                        isWaitingForInput = false;
                        isRbStream = false;
                        pendingLaunchPath = null;
                        lobbyPhase = 'none';
                        lobbyStopPolling();
                        _lobbyPrevButtons = {};
                        _lobbyActionCooldown = 0;
                        _isLobbyP1 = false;
                        const lobbyOverlayEl = document.getElementById('lobbyOverlay');
                        if (lobbyOverlayEl) lobbyOverlayEl.style.display = 'none';
                        const lobbyBannerEl = document.getElementById('lobbyBanner');
                        if (lobbyBannerEl) lobbyBannerEl.style.display = 'none';
        
                        // EN: Exit fullscreen if active
                        // FR: Quitter le plein écran si actif
                        if (document.fullscreenElement) {
                            try { document.exitFullscreen().catch(e => {}); } catch(e) {}
                        }
        
                        const lv = document.getElementById('listView');
                        const sv = document.getElementById('streamView');
                        const searchContainer = document.getElementById('publicSearchContainer');
                        const title = document.getElementById('viewTitle');
        
                        if (lv) lv.style.display = 'flex';
                        if (searchContainer) searchContainer.style.display = 'flex';
                        if (sv) sv.style.display = 'none';
                        if (title) title.innerText = t.viewTitleIdle;
        
                        const iframe = document.getElementById('streamIframe');
                        // EN: Unblock gamepad input in Moonlight iframe before clearing AND send STOP_STREAM
                        // FR: Débloquer les inputs manette dans l'iframe Moonlight avant de la vider ET envoyer STOP_STREAM
                        if (iframe && iframe.contentWindow) {
                            try { iframe.contentWindow.postMessage({type: 'BATRUN_LOBBY_INPUT_BLOCK', blocked: false}, '*'); } catch(e) {}
                            // EN: Send STOP_STREAM immediately then blank iframe after 3s (keep contentWindow alive)
                            // FR: Envoyer STOP_STREAM immédiatement puis vider iframe après 3s (garder contentWindow vivant)
                            try { iframe.contentWindow.postMessage({type: 'BATRUN_STOP_STREAM'}, '*'); } catch(e) {}
                            setTimeout(() => { try { iframe.src = 'about:blank'; } catch(e) {} }, 3000);
                        } else if (iframe) { iframe.src = 'about:blank'; }
        
                        // EN: Notify server that P1 session left the lobby
                        // FR: Notifier le serveur que la session P1 a quitté le lobby
                        if (token) {
                            try {
                                fetch(getRelayUrl('/api/public/lobby/leave'), {
                                    method: 'POST',
                                    headers: {'Content-Type': 'application/json'},
                                    body: JSON.stringify({ token: token, sessionId: _lobbySessionId })
                                });
                            } catch(e) {}
                        }
        
                        // EN: Refresh game list to reflect stopped state
                        // FR: Rafraîchir la liste des jeux pour refléter l'état arrêté
                        searchGames();
                        checkStatus();
                    }
                } catch (e) {
                    console.error('[EmergencyStop] Force-stop-session error:', e);
                }
            }

        async function checkStatus() {
            try {
                const ts = new Date().getTime();
                const token = localStorage.getItem('batrun_token') || '';
                const res = await fetch(getRelayUrl('/api/public/status?_t=' + ts + '&token=' + encodeURIComponent(token)));
                
                if (res.status === 401) {
                    console.warn('[BatRun] Session invalidated (user deleted or expired). Clearing tokens and redirecting...');
                    // EN: Clear stale tokens and redirect with flag to ensure /connect cleans up too
                    // FR: Nettoyer les tokens périmés et rediriger avec le flag pour assurer le nettoyage sur /connect
                    localStorage.removeItem('batrun_token');
                    document.cookie = 'batrun_token=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
                    window.location.replace('/connect?logout=1');
                    return;
                }
                
                if(!res.ok) return;
                const status = await res.json();
                
                // [BATRUN-MOD]: Capture if game just finished BEFORE updating local timestamps
                const _justFinishedNormal = (status.lastGameEndTime && status.lastGameEndTime > localLastGameEndTime);
                
                // EN: Read previous busy state before overwrite / FR: Lire l'état busy précédent avant écrasement
                const previousBusy = globalStatus ? globalStatus.isGameInProgress : false;

                // [BATRUN-FORK]: Update local globalStatus ref
                globalStatus = status;
                
                const lv = document.getElementById('listView');
                const sv = document.getElementById('streamView');
                const title = document.getElementById('viewTitle');
                const searchContainer = document.getElementById('publicSearchContainer');
                
                const t = publicTranslations[currentLang];
              
                // [BATRUN] EN: Detect force-stop signal from server (works even when isGameInProgress was already false, e.g. on ES interface)
                // FR: Détecter le signal d'arrêt forcé du serveur (fonctionne même si isGameInProgress était déjà false, ex: sur l'interface ES)
                if (status.forceStopTime && status.forceStopTime > localForceStopTime) {
                    const isFirstSyncFS = (localForceStopTime === 0);
                    localForceStopTime = status.forceStopTime;
                    if (!isFirstSyncFS && (isStreaming || isRbStream)) {
                        console.log('[BatRun] Force-stop signal detected from server (timestamp=' + status.forceStopTime + '). Closing stream session.');

                        // EN: Force-close this client's stream session immediately
                        // FR: Fermer immédiatement la session stream de ce client
                        isStreaming = false;
                        isRbStream = false;
                        isWaitingForML = false;
                        isWaitingForInput = false;
                        pendingLaunchPath = null;
                        // EN: Reset session game started flag / FR: Réinitialiser le flag de jeu démarré de la session
                        _sessionGameStarted = false;

                        // [BATRUN-v3]: Reset lobby state on force-stop (this is an explicit stop action, unlike passive stream-end)
                        // FR: Réinitialiser le lobby lors d'un arrêt forcé (c'est une action explicite, pas une fin passive de stream)
                        lobbyPhase = 'none';
                        lobbyStopPolling();
                        _lobbyPrevButtons = {};
                        _lobbyActionCooldown = 0;
                        _isLobbyP1 = false;
                        _emergencyConfirmShown = false;
                        const lobbyOverlayEl = document.getElementById('lobbyOverlay');
                        if (lobbyOverlayEl) lobbyOverlayEl.style.display = 'none';
                        const lobbyBannerEl = document.getElementById('lobbyBanner');
                        if (lobbyBannerEl) lobbyBannerEl.style.display = 'none';

                        if (document.fullscreenElement) {
                            try { document.exitFullscreen().catch(e => {}); } catch(e) {}
                        }

                        const _fsIframe = document.getElementById('streamIframe');
                        if (_fsIframe && _fsIframe.contentWindow) {
                            try { _fsIframe.contentWindow.postMessage({type: 'BATRUN_LOBBY_INPUT_BLOCK', blocked: false}, '*'); } catch(e) {}
                            // EN: Send STOP_STREAM immediately then blank iframe after 3s (keep contentWindow alive)
                            // FR: Envoyer STOP_STREAM immédiatement puis vider iframe après 3s (garder contentWindow vivant)
                            try { _fsIframe.contentWindow.postMessage({type: 'BATRUN_STOP_STREAM'}, '*'); } catch(e) {}
                            setTimeout(() => { try { _fsIframe.src = 'about:blank'; } catch(e) {} }, 3000);
                        }

                        lv.style.display = 'flex';
                        searchContainer.style.display = 'flex';
                        sv.style.display = 'none';
                        title.innerText = t.viewTitleIdle;
                        document.getElementById('streamIframe').src = 'about:blank';

                        // EN: Notify server that this session left the lobby
                        // FR: Notifier le serveur que cette session a quitté le lobby
                        const _fsToken = localStorage.getItem('batrun_token');
                        if (_fsToken) {
                            try {
                                fetch(getRelayUrl('/api/public/lobby/leave'), {
                                    method: 'POST',
                                    headers: {'Content-Type': 'application/json'},
                                    body: JSON.stringify({ token: _fsToken, sessionId: _lobbySessionId })
                                });
                            } catch(e) {}
                        }

                        searchGames();
                    }
                }
              
                // Re-render chunk if busy status changed to update buttons lock state
                // Re-render chunk if busy status changed to update buttons lock state
                if (!status.isGameInProgress && previousBusy) {
                    searchGames();
                }

                // EN: Show/hide stop session button in lobby header
                // FR: Afficher/masquer le bouton d'arrêt de session dans le header du lobby
                const stopBtn = document.getElementById('btnStopSessionLobby');
                if (stopBtn) {
                    stopBtn.style.display = ((status.isGameInProgress && status.isWebLaunch) || status.isLobbyWaiting) ? 'inline-block' : 'none';
                }

                // [BATRUN-v3]: Initialization success detection.
                // We consider initialization successful if the server reports any active state (Lobby, Launching, or Game).
                // This ensures the ""Retry"" modal is suppressed as soon as we reach the lobby.
                // FR: Détection du succès de l'initialisation.
                // On considère que l'initialisation a réussi si le serveur rapporte un état actif (Lobby, Lancement ou Jeu).
                // Cela garantit la coupure du mécanisme de ""Retry"" dès que le lobby est atteint.
                const _backendActive = status.isGameInProgress || status.isWebLaunch || status.isLobbyActive || status.isLobbyWaiting;
                
                if (_backendActive) {
                    // [BATRUN-FORK-v11]: Don't clear isWaitingForML if we're joining an existing session.
                    // We need it to stay true until ML_CONNECTED arrives to trigger the ""Press any button"" overlay.
                    // FR: Ne pas effacer isWaitingForML si on rejoint une session existante.
                    // On en a besoin pour que ML_CONNECTED déclenche l'overlay ""Appuyez sur un bouton"".
                    if (isWaitingForML && pendingLaunchPath !== '[JOIN]') isWaitingForML = false;
                    if (isWaitingForInput) isWaitingForInput = false;
                    _sessionGameStarted = true; 
                }
                
                // [BATRUN-MOD]: If lobby just finished, it means we passed the start phase.
                if (lobbyPhase !== 'none' && !status.isLobbyActive) {
                    isWaitingForML = false;
                    isWaitingForInput = false;
                }

                // FR: Annulation déterministe si un événement GAME_END s'est produit entre deux requêtes de status
                if (status.lastGameEndTime && status.lastGameEndTime > localLastGameEndTime) {
                    const isFirstSyncGE = (localLastGameEndTime === 0);
                    localLastGameEndTime = status.lastGameEndTime;
                    if (!isFirstSyncGE && (isWaitingForML || isWaitingForInput)) {
                        console.log(""[BatRun] GAME_END detecté via timestamp. Annulation de l'attente ML/Input."");
                        isWaitingForML = false;
                        isWaitingForInput = false;
                    }
                // [BATRUN-FORK-v9]: When game ends, reset lobby phase BOTH client-side AND server-side.
                // Previously, only the client-side lobbyPhase was reset, but the server kept the lobby
                // in phase 'launching'. On the next game launch, triggerLaunch() would find this stale
                // lobby and join it (phase=launching), causing the game to start immediately without
                // showing the lobby overlay. Now we also call /lobby/leave to reset the server lobby.
                // FR: Quand le jeu se termine, réinitialiser le lobby côté client ET côté serveur.
                // Avant, seul lobbyPhase côté client était reset, mais le serveur gardait le lobby
                // en phase 'launching'. Au prochain lancement, triggerLaunch() trouvait ce lobby périmé
                // et le rejoignait (phase=launching), causant le démarrage immédiat sans overlay lobby.
                const _laEnd = (typeof lobbyPhase !== 'undefined' && lobbyPhase !== 'none');
                if (_laEnd && !status.isGameInProgress) {
                console.log('[BatRun] GAME_END detected, no game in progress — resetting lobby phase (client + server).');
                lobbyPhase = 'none';
                lobbyStopPolling();
                _lobbyPrevButtons = {};
                _lobbyActionCooldown = 0;
                _isLobbyP1 = false;
                const _loEl = document.getElementById('lobbyOverlay');
                if (_loEl) _loEl.style.display = 'none';
                const _lbEl = document.getElementById('lobbyBanner');
                if (_lbEl) _lbEl.style.display = 'none';
                // [BATRUN-FORK-v9]: Also notify server to reset the lobby so the next launch
                // gets a fresh lobby in 'lobby' phase instead of finding a stale 'launching' lobby.
                // FR: Notifier aussi le serveur de réinitialiser le lobby pour que le prochain lancement
                // obtienne un nouveau lobby en phase 'lobby' au lieu de trouver un lobby périmé 'launching'.
                const _geToken = localStorage.getItem('batrun_token');
                if (_geToken) {
                try {
                fetch(getRelayUrl('/api/public/lobby/leave'), {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify({ token: _geToken, sessionId: _lobbySessionId })
                }).catch(() => {});
                } catch(e) {}
                }
                }
                }
                
                // [BATRUN-FORK-v6]: Update the lobby join banner based on current status
                // FR: Mettre a jour la banniere de rejoindre le lobby selon le statut actuel
                updateLobbyJoinBanner();
                
                // [BATRUN-FORK-v3]: Don't cancel isWaitingForML if lobby is active.
                // The lobby phase is a normal waiting state — the user is waiting for other players
                // or pressing Start. The 20s timeout should only fire if the system truly crashed
                // without GAME_END, NOT during a legitimate lobby wait.
                // FR: Ne pas annuler isWaitingForML si le lobby est actif.
                // La phase de lobby est un état d'attente normal. Le timeout de 20s ne doit se
                // déclencher que si le système a vraiment crashé sans envoyer GAME_END.
                // [BATRUN-FORK-v10]: Consider 'launching' and 'confirm' phases as active to prevent stream drop.
                // FR: Considérer les phases 'launching' et 'confirm' comme actives pour éviter l'arrêt du stream.
                const _lobbyIsActive = (typeof lobbyPhase !== 'undefined' && lobbyPhase !== 'none');
                
                // [BATRUN-MOD]: If lobby is active, it means stream is working. Reset input waiting flag.
                if (_lobbyIsActive && isWaitingForInput) {
                    isWaitingForInput = false;
                }

                // [BATRUN-MOD]: Auto-reset input waiting flag after 20s of streaming.
                // This prevents false positives on normal end for games that don't report PID correctly.
                if (isStreaming && isWaitingForInput) {
                    const _now = new Date().getTime();
                    if (_now - waitStartTimestamp > 20000) {
                        isWaitingForInput = false;
                    }
                }

                // [BATRUN-MOD]: Initial connection timeout check (especially for Mobile/VPN)
                if (isWaitingForML && !_justFinishedNormal && !_sessionGameStarted) {
                    const _now = new Date().getTime();
                    if (_now - waitStartTimestamp > 30000) {
                        console.log('[BatRun] Initial connection timeout (30s). Resetting wait state.');
                        // [BATRUN-v3]: Temporarily disabled as requested / FR: Désactivé temporairement à la demande
                        // showRetryModal();
                        isWaitingForML = false;
                        pendingLaunchPath = null;
                        return;
                    }
                }

                const _serverBusy = status.isWebLaunch || status.isGameInProgress || isRbStream || _lobbyIsActive;
                const _recentlyStarted = isWaitingForML && (new Date().getTime() - waitStartTimestamp < 5000); // 5s grace period for server to report busy

                if ((_serverBusy || _recentlyStarted) && status.isMoonlightEnabled) {
                    if (!isStreaming) {
                        isStreaming = true;
                        lv.style.display = 'none';
                        searchContainer.style.display = 'none';
                        sv.style.display = 'block';
                        title.innerText = t.viewTitleStream;

                        const token = localStorage.getItem('batrun_token') || '';
                        const streamUrl = `${window.location.protocol}//${window.location.host}/api/moonlight-auth?token=${encodeURIComponent(token)}&hostId=auto&appId=0`;
                        const iframe = document.getElementById('streamIframe');
                        batrunStartStream(iframe, streamUrl, () => {
                        // [BATRUN-FORK]: Force focus to iframe so it captures gamepad inputs immediately
                        setTimeout(() => {
                        iframe.focus();
                        // Ensure clicking the view restores focus to the iframe
                        sv.onclick = () => iframe.focus();
                        }, 1000);
                        });
                    }
                } else {
                    if (isStreaming) {
                        // [BATRUN-FORK-v3]: Auto-reconnect if stream drops while lobby is active or game is still in progress.
                        // When the Moonlight WebRTC stream disconnects (ICE timeout) but the game is still running
                        // on the host or we're in a lobby phase, we should NOT return to the game list.
                        // Instead, we set isStreaming=false so the next checkStatus() cycle will
                        // automatically re-bridge the Moonlight session (the iframe is re-created).
                        // FR: Reconnexion automatique si le stream s'arrête alors que le lobby est actif
                        // ou que le jeu est toujours en cours. Quand le stream Moonlight WebRTC se déconnecte
                        // (timeout ICE) mais que le jeu tourne toujours ou qu'on est en lobby, on ne doit PAS
                        // retourner à la liste des jeux. On remet isStreaming=false pour que le prochain
                        // checkStatus() re-crée automatiquement l'iframe du stream.
                        // [BATRUN-FORK-v10]: Stay in stream during transition phases
                        // FR: Rester en stream pendant les phases de transition
                        const _lobbyStillActive = (typeof lobbyPhase !== 'undefined' && lobbyPhase !== 'none');
                        const _gameStillRunning = status.isGameInProgress && status.isWebLaunch;
        
                        if ((isWaitingForML || isWaitingForInput) && !_justFinishedNormal) {
                            if (_sessionGameStarted) {
                                console.log('[BatRun] Stream ended. Game was already started once. Suppressing retry modal.');
                            } else {
                                console.log('[BatRun] Stream ended unexpectedly during initialization. Retry modal disabled.');
                                // [BATRUN-v3]: Temporarily disabled as requested / FR: Désactivé temporairement à la demande
                                // showRetryModal();
                                return;
                            }
                        }

                        if (_lobbyStillActive || _gameStillRunning) {
                            console.log('[BatRun] Stream ended but lobby/game still active. Auto-reconnecting... (lobby=' + lobbyPhase + ', gameInProgress=' + _gameStillRunning + ')');
                            isStreaming = false; // Reset so next checkStatus() re-creates the iframe
                            // Don't reset isWaitingForML, isRbStream, pendingLaunchPath, or lobby state
                            // Just reset isStreaming so the stream view is re-initialized on next poll
        
                            // EN: Unblock gamepad input in Moonlight iframe (old one is dying)
                            // FR: Débloquer les inputs manette dans l'iframe Moonlight (l'ancienne se termine)
                            const _endIframe = document.getElementById('streamIframe');
                            if (_endIframe && _endIframe.contentWindow) {
                                try { _endIframe.contentWindow.postMessage({type: 'BATRUN_LOBBY_INPUT_BLOCK', blocked: false}, '*'); } catch(e) {}
                            }
        
                            // Keep stream view visible with a ""reconnecting"" message
                            sv.style.display = 'block';
                            lv.style.display = 'none';
                            searchContainer.style.display = 'none';
                            title.innerText = '⌛ Reconnexion...';
                            document.getElementById('streamIframe').src = 'about:blank';
                        } else {

                            console.log('[BatRun] Stream ended and no game/lobby active. Returning to list.');
                            isStreaming = false;
                            isRbStream = false;
                            isWaitingForML = false;
                            isWaitingForInput = false;
                            pendingLaunchPath = null;
                            // EN: Reset session game started flag / FR: Réinitialiser le flag de jeu démarré de la session
                            _sessionGameStarted = false;
        
                            // [BATRUN-FORK-v3]: Do NOT reset lobby or send lobby/leave when stream ends.
                            // The lobby lifecycle is now fully managed by server-side state and lobbyPollStatus().
                            // FR: Ne PAS réinitialiser le lobby ni envoyer lobby/leave quand le stream se termine.
                            // Le cycle de vie du lobby est désormais entièrement géré par l'état serveur et lobbyPollStatus().
        
                            // [BATRUN-FORK]: Automatically exit fullscreen when stream ends
                            if (document.fullscreenElement) {
                                try { document.exitFullscreen().catch(e => {}); } catch(e) {}
                            }
        
                            // EN: Unblock gamepad + send STOP_STREAM, then blank iframe after 3s
                            //     IMPORTANT: keep a local ref to the iframe and set src INSIDE setTimeout.
                            //     If src is set before setTimeout fires, contentWindow becomes null.
                            // FR: Débloquer manette + envoyer STOP_STREAM, puis vider iframe après 3s
                            //     IMPORTANT: garder une ref locale à l'iframe et mettre src DANS setTimeout.
                            //     Si src est vidé avant que le setTimeout se déclenche, contentWindow devient null.
                            const _endIframe2 = document.getElementById('streamIframe');
                            if (_endIframe2 && _endIframe2.contentWindow) {
                                try { _endIframe2.contentWindow.postMessage({type: 'BATRUN_LOBBY_INPUT_BLOCK', blocked: false}, '*'); } catch(e) {}
                                // EN: Send STOP_STREAM immediately — Moonlight internally calls stopCurrentSession()
                                //     which cleanly disconnects the virtual controller within 3s.
                                // FR: Envoyer STOP_STREAM immédiatement — Moonlight appelle stopCurrentSession()
                                //     qui déconnecte proprement la manette virtuelle en moins de 3s.
                                try { _endIframe2.contentWindow.postMessage({type: 'BATRUN_STOP_STREAM'}, '*'); } catch(e) {}
                                // EN: Blank the iframe after 3s so Moonlight has time to clean up.
                                // FR: Vider l'iframe après 3s pour laisser Moonlight le temps de nettoyer.
                                setTimeout(() => { try { _endIframe2.src = 'about:blank'; } catch(e) {} }, 3000);
                            }
        
                            lv.style.display = 'flex';
                            searchContainer.style.display = 'flex';
                            sv.style.display = 'none';
                            title.innerText = t.viewTitleIdle;
        
                            // EN: Refresh game list to reflect current state
                            // FR: Rafraîchir la liste des jeux pour refléter l'état actuel
                            searchGames();
                        }
                    }
                }
            } catch(e) {}
        }

        function startPolling() {
            setInterval(checkStatus, 3000);
            checkStatus();
        }

        function logout() {
            // [BATRUN-GUEST] EN: Notify server to invalidate token and delete guest accounts
            // FR: Notifier le serveur pour invalider le token et supprimer les comptes guest
            const token = localStorage.getItem('batrun_token') || '';
            if (token) {
                try {
                    // EN: Fire-and-forget — don't block redirect on network failure
                    // FR: Fire-and-forget — ne pas bloquer la redirection en cas d'erreur reseau
                    navigator.sendBeacon('/api/public/logout', JSON.stringify({ token: token }));
                } catch(e) { console.warn('[BatRun] Logout beacon failed:', e); }
            }
            localStorage.removeItem('batrun_token');
            document.cookie = 'batrun_token=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
            window.location.href = '/connect';
        }

        // [BATRUN-CRED] EN: Route handling based on token presence
        // FR: Gestion du routage basé sur la présence du token
        function getCookie(name) {
            const value = '; ' + document.cookie;
            const parts = value.split('; ' + name + '=');
            if (parts.length === 2) return parts.pop().split(';').shift();
            return null;
        }

        let batrunToken = localStorage.getItem('batrun_token') || getCookie('batrun_token');
        
        // EN: Server already validated token before serving this page
        // FR: Le serveur a deja valide le token avant de servir cette page
        if (!batrunToken) {
            window.location.replace('/connect');
        } else {
            localStorage.setItem('batrun_token', batrunToken);
            showMainUI();
        }
        // Initialize display
        setLanguage(currentLang);

        // [BATRUN] EN: Hide mouse cursor in fullscreen after 3s of inactivity / FR: Masquer le curseur souris en plein écran
        let _cursorHideTimer = null;
        function resetCursorTimer() {
            document.body.style.cursor = '';
            clearTimeout(_cursorHideTimer);
            if (document.fullscreenElement) {
                _cursorHideTimer = setTimeout(() => { document.body.style.cursor = 'none'; }, 3000);
            }
        }
        document.addEventListener('mousemove', resetCursorTimer);
        function syncFullscreenUI() {
            const btnFull = document.getElementById('btnFullStream');
            if (document.fullscreenElement) {
                resetCursorTimer();
                if (btnFull) btnFull.style.setProperty('display', 'none', 'important');
            } else {
                clearTimeout(_cursorHideTimer);
                document.body.style.cursor = '';
                if (btnFull) btnFull.style.setProperty('display', 'block');
            }
        }
        document.addEventListener('fullscreenchange', syncFullscreenUI);
        document.addEventListener('webkitfullscreenchange', syncFullscreenUI);
        document.addEventListener('mozfullscreenchange', syncFullscreenUI);
        document.addEventListener('msfullscreenchange', syncFullscreenUI);


        // [BATRUN] EN: Inject CSS into Moonlight iframe to hide native Moonlight overlay / FR: Masquer la flèche overlay Moonlight dans l'iframe
        document.getElementById('streamIframe').addEventListener('load', function() {
            try {
                const iframeDoc = this.contentDocument || this.contentWindow.document;
                const s = iframeDoc.createElement('style');
                s.textContent = '#sidebarButton, .sidebar-toggle, .arrow-toggle { display: none !important; }';
                iframeDoc.head.appendChild(s);
            } catch(e) { /* cross-origin: cannot inject */ }
        });

        // [BATRUN] Node Selection Logic
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
            } else if (nodeEl && nodeEl.dataset.requiresLogin !== undefined) {
                requiresLogin = nodeEl.dataset.requiresLogin === 'true';
            }
            updateAuthUI();

            // [BATRUN-MOD]: Auto-jump gamepad focus to START button if Login is disabled
            if (!initial && _gpActive && !requiresLogin && document.getElementById('authContainer') && document.getElementById('authContainer').style.display !== 'none') {
                setTimeout(() => {
                    const els = getPubFocusables();
                    const btnG = document.getElementById('btnGuestStart');
                    if (btnG) {
                        const idx = els.indexOf(btnG);
                        if (idx >= 0) gpSetFocus(idx);
                    }
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
            // On s'assure de la priorité en vérifiant isExternalClient en premier.
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

        // [BATRUN] Auto-force fullscreen on first user interaction for /cloud
        let _hasForcedFullscreen = false;
        function autoForceFullscreen() {
            if (!_hasForcedFullscreen && !document.fullscreenElement) {
                try {
                    document.documentElement.requestFullscreen().then(() => {
                        localStorage.setItem('batrun_fullscreen', 'true');
                    }).catch(e => {});
                    _hasForcedFullscreen = true;
                } catch(e) {}
            }
            document.removeEventListener('click', autoForceFullscreen);
            document.removeEventListener('keydown', autoForceFullscreen);
            document.removeEventListener('touchstart', autoForceFullscreen);
        }
        document.addEventListener('click', autoForceFullscreen);
        document.addEventListener('keydown', autoForceFullscreen);
        document.addEventListener('touchstart', autoForceFullscreen);

        loadNodes();
        setInterval(loadNodes, 5000);

        // [BATRUN-CRED] EN: If redirected back after a failed form login, show error message now that translations are loaded
        // FR: Si redirigé après un échec de connexion, afficher le message d'erreur (traductions chargées)
    </script>
</body>
</html>";
            bool effectiveRequiresLogin = _manager.PublicAccessRequiresLogin || isExternal;
            return html
                .Replace("{REQUIRES_LOGIN_JS}", effectiveRequiresLogin.ToString().ToLower())
                .Replace("{IS_EXTERNAL_JS}", isExternal.ToString().ToLower());
        }
    }
}
