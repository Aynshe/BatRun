// EN: Partial class for ArcadeApiService - Admin page HTML generation
// FR: Classe partielle pour ArcadeApiService - Generation HTML de la page Admin
// EN: This file contains the GetWebUIHtml() method
// FR: Ce fichier contient la methode GetWebUIHtml()

namespace BatRun.Core
{
    public partial class ArcadeApiService
    {
        private string GetWebUIHtml()
        {
            return @"<!DOCTYPE html>
<html lang=""fr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>BatRun - Operator Dashboard</title>
    <style>
        :root {
            --primary: #00d2ff;
            --secondary: #3a7bd5;
            --bg: #0f0c29;
            --card: rgba(255, 255, 255, 0.05);
            --text: #ffffff;
            --danger: #ff416c;
            --success: #00b09b;
        }

        .status-badge {
            display: inline-block; padding: 4px 10px; border-radius: 20px; font-weight: bold; margin-left: 8px; font-size: 0.7em; 
            vertical-align: middle; visibility: hidden; text-transform: uppercase; letter-spacing: 1px;
        }
        .badge-freeplay { background: var(--success); color: white; box-shadow: 0 0 10px rgba(0, 176, 155, 0.4); }
        .badge-operator { background: #f39c12; color: white; box-shadow: 0 0 10px rgba(243, 156, 18, 0.4); animation: pulse-op 2s infinite; }
        
        /* Game Launcher Styles */
        .search-container { 
            width: 100%; 
            margin-top: 20px; 
            display: flex; 
            align-items: center; 
            gap: 15px; 
            background: var(--bg);
            padding: 10px 0;
            z-index: 100;
        }
        .search-container.sticky {
            position: sticky;
            top: 0;
            border-bottom: 2px solid var(--primary);
            box-shadow: 0 10px 30px rgba(0,0,0,0.5);
            padding-left: 10px;
            padding-right: 10px;
        }
        select#systemFilter {
            flex: 0 0 auto;
            width: 110px;
            height: 38px;
            background-color: #1a1a2e;
            border: 1px solid rgba(255,255,255,0.2);
            border-radius: 10px;
            color: white;
            padding: 0 12px;
            outline: none;
            cursor: pointer;
            font-size: 0.85rem;
            appearance: none;
            -webkit-appearance: none;
            background-image: url(""data:image/svg+xml;charset=US-ASCII,%3Csvg%20xmlns%3D%22http%3A%2F%2Fwww.w3.org%2F2000%2Fsvg%22%20width%3D%22292.4%22%20height%3D%22292.4%22%3E%3Cpath%20fill%3D%22%2300d2ff%22%20d%3D%22M287%2069.4a17.6%2017.6%200%200%200-13-5.4H18.4c-5%200-9.3%201.8-12.9%205.4A17.6%2017.6%200%200%200%200%2082.2c0%205%201.8%209.3%205.4%2012.9l128%20127.9c3.6%203.6%207.8%205.4%2012.8%205.4s9.2-1.8%2012.8-5.4L287%2095c3.5-3.5%205.4-7.8%205.4-12.8%200-5-1.9-9.2-5.5-12.8z%22%2F%3E%3C%2Fsvg%3E"");
            background-repeat: no-repeat;
            background-position: right 10px top 50%;
            background-size: 10px auto;
            padding-right: 30px;
            transition: border-color 0.2s;
        }
        select#systemFilter option, select#logDateSelect option {
            background-color: #161625;
            color: white;
            padding: 10px;
        }
        select#systemFilter:focus, select#logDateSelect:focus { border-color: var(--primary); box-shadow: 0 0 8px rgba(0, 210, 255, 0.3); }

        /* Back to top button */
        #backToTop {
            position: fixed;
            bottom: 20px;
            right: 20px;
            width: 50px;
            height: 50px;
            border-radius: 50%;
            background: var(--primary);
            color: white;
            border: none;
            cursor: pointer;
            display: none;
            align-items: center;
            justify-content: center;
            font-size: 1.5rem;
            z-index: 1000;
            box-shadow: 0 4px 15px rgba(0,0,0,0.5);
            transition: transform 0.2s, background 0.2s;
        }
        #backToTop:hover { transform: scale(1.1); background: #fff; color: var(--primary); }
        #backToTop.show { display: flex; animation: fadeIn 0.3s; }

        @keyframes fadeIn { from { opacity: 0; transform: translateY(20px); } to { opacity: 1; transform: translateY(0); } }

        .search-results { margin-top: 20px; display: grid; grid-template-columns: repeat(auto-fill, minmax(180px, 1fr)); gap: 15px; width: 100%; }
        .game-item { background: var(--card); border-radius: 12px; padding: 12px; cursor: pointer; transition: all 0.2s; border: 1px solid rgba(255,255,255,0.05); text-align: left; position: relative; overflow: hidden; }
        .game-item:hover { border-color: var(--primary); background: rgba(255,255,255,0.1); transform: translateY(-3px); }
        .game-item img { width: 100%; border-radius: 8px; margin-bottom: 8px; aspect-ratio: 3/4; object-fit: contain; background: rgba(0,0,0,0.2); }
        .game-play-btn { position: absolute; bottom: 50px; right: 15px; background: var(--primary); color: white; width: 36px; height: 36px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 1.2rem; cursor: pointer; opacity: 0; transition: opacity 0.2s, transform 0.2s; box-shadow: 0 4px 10px rgba(0,0,0,0.8); z-index: 10; }
        .game-item:hover .game-play-btn { opacity: 1; }
        .game-play-btn:hover { transform: scale(1.15); background: #fff; color: var(--primary); }
        /* EN: Locked state when a game is already running / FR: Etat verrouillé si un jeu est déjà lancé */
        .game-play-btn.disabled-btn { background: rgba(255,65,108,0.35) !important; cursor: not-allowed !important; font-size: 0.9rem !important; opacity: 0.85 !important; }
        .game-play-btn.disabled-btn:hover { transform: none !important; background: rgba(255,65,108,0.5) !important; color: white !important; }
        @media (pointer: coarse) { .game-play-btn { opacity: 0.85 !important; } }
        .game-item-title { font-size: 0.85rem; font-weight: bold; margin-bottom: 4px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
        .game-item-system { font-size: 0.65rem; color: var(--primary); text-transform: uppercase; font-weight: 800; }
        
        /* Video Player Specific Styles */
        video#mdVideo {
            background: #000;
            box-shadow: 0 10px 30px rgba(0,0,0,0.5);
            border: 1px solid rgba(255,255,255,0.1);
            transition: transform 0.3s;
        }
        video#mdVideo:hover {
            transform: scale(1.02);
            border-color: var(--primary);
        }
        
        .machine-selector { display: flex; flex-wrap: wrap; gap: 10px; margin-top: 20px; background: rgba(0,0,0,0.2); padding: 15px; border-radius: 12px; border: 1px solid rgba(255,255,255,0.05); justify-content: center; }
        .machine-toggle { display: flex; align-items: center; gap: 8px; background: rgba(255,255,255,0.05); padding: 8px 12px; border-radius: 8px; cursor: pointer; border: 1px solid transparent; transition: all 0.2s; font-size: 0.8rem; }
        .machine-toggle.active { border-color: var(--primary); background: rgba(0,210,255,0.1); box-shadow: 0 0 10px rgba(0, 210, 255, 0.2); }
        .machine-toggle input { display: none; }
        .machine-toggle .dot { width: 8px; height: 8px; border-radius: 50%; background: #555; }
        .machine-toggle.active .dot { background: var(--primary); box-shadow: 0 0 5px var(--primary); }
        
        .es-warning { background: rgba(255, 65, 108, 0.1); border: 1px solid var(--danger); padding: 15px; border-radius: 12px; margin-top: 20px; display: none; text-align: left; font-size: 0.9rem; }
        .availability-dot { width: 10px; height: 10px; border-radius: 50%; display: inline-block; margin-right: 5px; background: #555; }
        .availability-dot.available { background: var(--success); box-shadow: 0 0 8px var(--success); }
        
        .playing-badge {
            position: absolute; top: 10px; left: 10px;
            background: linear-gradient(135deg, #ff416c, #ff4b2b);
            color: white; padding: 2px 8px; border-radius: 4px;
            font-size: 0.65rem; font-weight: 800; font-variant-numeric: tabular-nums;
            box-shadow: 0 0 10px rgba(255, 65, 108, 0.5);
            animation: pulse-playing 1.5s infinite;
            z-index: 5; pointer-events: none;
            display: flex; align-items: center; gap: 4px;
        }

        .playing-badge::before {
            content: ""; width: 6px; height: 6px; background: white; border-radius: 50%;
        }

        @keyframes pulse-playing {
            0% { transform: scale(1); opacity: 0.9; }
            50% { transform: scale(1.05); opacity: 1; }
            100% { transform: scale(1); opacity: 0.9; }
        }

        /* EN: Multi-machine banner styles / FR: Styles du bandeau multi-bornes */
        .banner-details { display: none; flex-direction: column; gap: 8px; margin-top: 12px; border-top: 1px solid rgba(255,255,255,0.15); padding-top: 12px; width: 100%; }
        .banner-machine-row { display: flex; align-items: center; justify-content: space-between; font-size: 0.85rem; padding: 8px 12px; background: rgba(0,0,0,0.25); border-radius: 8px; border: 1px solid rgba(255,255,255,0.05); }
        .banner-machine-info { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 2px; }
        .banner-machine-name { font-weight: 800; color: var(--primary); font-size: 0.7rem; text-transform: uppercase; letter-spacing: 1px; }
        .banner-machine-game { font-weight: bold; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; color: #fff; }
        .banner-toggle-btn { background: rgba(255,255,255,0.1); border: 1px solid rgba(255,255,255,0.2); color: white; border-radius: 50%; width: 30px; height: 30px; display: flex; align-items: center; justify-content: center; cursor: pointer; transition: all 0.2s; font-size: 0.7rem; }
        .banner-toggle-btn:hover { background: var(--primary); border-color: #fff; transform: scale(1.1); }
        .banner-toggle-btn.active { transform: rotate(180deg); background: var(--primary); }
        
        /* Impact Effect / Effet d'Impact */
        .crush-impact {
            animation: crush-impact-anim 0.6s cubic-bezier(0.4, 0, 0.2, 1) forwards !important;
            pointer-events: none !important;
        }

        @keyframes crush-impact-anim {
            0% { transform: scale(1); opacity: 1; filter: blur(0px); }
            100% { transform: scale(4); opacity: 0; filter: blur(15px); }
        }
        
        .search-container {
            display: flex !important;
            flex-direction: row !important;
            flex-wrap: wrap !important;
            align-items: center !important;
            gap: 10px;
            background: rgba(0,0,0,0.3) !important;
            backdrop-filter: blur(10px);
            padding: 12px 15px;
            border-radius: 15px;
            border: 1px solid rgba(255,255,255,0.1);
            margin-bottom: 20px;
            position: sticky;
            top: 10px;
            z-index: 100;
        }

        .search-filter-group {
            display: flex;
            gap: 8px;
            flex: 1 1 auto;
        }

        .search-main-group {
            display: flex;
            gap: 8px;
            flex: 2 1 300px; /* Trigger wrap on small screens / Déclenche le saut de ligne sur petit écrans */
        }

        .search-container select {
            background: rgba(0,0,0,0.3) !important;
            border: 1px solid rgba(255,255,255,0.1) !important;
            border-radius: 8px !important;
            color: white !important;
            padding: 8px 12px !important;
            font-size: 0.85rem !important;
            outline: none;
            width: auto !important;
            min-width: 100px;
            flex: 1;
            max-width: 180px;
            cursor: pointer;
            -webkit-appearance: none;
            appearance: none;
            background-image: url(""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' fill='white' viewBox='0 0 16 16'%3E%3Cpath d='M7.247 11.14 2.451 5.658C1.885 5.013 2.345 4 3.204 4h9.592a1 1 0 0 1 .753 1.659l-4.796 5.48a1 1 0 0 1-1.506 0z'/%3E%3C/svg%3E"") !important;
            background-repeat: no-repeat !important;
            background-position: right 8px center !important;
            padding-right: 25px !important;
        }

        .search-container select:hover { border-color: var(--primary) !important; background-color: rgba(255,255,255,0.1) !important; }
        .search-container select option { background: #1a1a1a; color: white; }

        .search-input-group {
            display: flex;
            flex: 1;
            background: rgba(255,255,255,0.05);
            border: 1px solid rgba(255,255,255,0.1);
            border-radius: 10px;
            padding: 2px 10px;
            transition: all 0.2s;
        }
        
        .search-row {
            display: flex;
            gap: 10px;
            width: 100%;
            align-items: center;
        }

        .search-input-group {
            display: flex;
            flex: 1;
            background: rgba(255,255,255,0.05);
            border: 1px solid rgba(255,255,255,0.1);
            border-radius: 10px;
            padding: 2px 5px;
            transition: border-color 0.2s;
        }
        .search-input-group:focus-within { border-color: var(--primary); }
        .search-input-group input { 
            background: transparent !important; 
            border: none !important; 
            margin: 0 !important; 
            text-align: left !important;
            font-size: 1rem !important;
        }

        @keyframes pulse-op {
            0% { opacity: 0.8; transform: scale(1); }
            50% { opacity: 1; transform: scale(1.05); }
            100% { opacity: 0.8; transform: scale(1); }
        }

        * { margin: 0; padding: 0; box-sizing: border-box; font-family: 'Segoe UI', system-ui, sans-serif; }
        
        body {
            background: linear-gradient(135deg, #0f0c29, #302b63, #24243e);
            min-height: 100vh;
            color: var(--text);
            display: flex;
            flex-direction: column;
            align-items: center;
            padding: 20px;
        }

        .container {
            width: 100%;
            max-width: 800px;
            animation: fadeIn 0.8s ease-out;
            display: none;
            position: relative;
        }

        .network-grid {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
            gap: 20px;
            width: 100%;
            margin-top: 20px;
            justify-content: center;
        }

        .machine-card {
            background: var(--card);
            backdrop-filter: blur(10px);
            border: 1px solid rgba(255, 255, 255, 0.1);
            border-radius: 20px;
            padding: 20px;
            text-align: center;
            cursor: pointer;
            transition: all 0.3s;
            position: relative;
            overflow: hidden;
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: 12px;
            min-height: 240px;
            justify-content: flex-start;
        }

        .machine-card:hover { 
            transform: translateY(-5px); 
            border-color: var(--primary);
            background: rgba(255, 255, 255, 0.1);
        }

        .machine-card.offline { opacity: 0.6; filter: grayscale(0.8); }

        /* EN: Three-dots button for machine properties / FR: Bouton trois points pour les propriétés */
        .machine-props-btn {
            position: absolute; top: 10px; right: 10px;
            background: rgba(255,255,255,0.1); border: 1px solid rgba(255,255,255,0.2);
            border-radius: 8px; color: white; font-size: 1.3rem; line-height: 1;
            padding: 2px 8px; cursor: pointer; z-index: 2;
            transition: background 0.2s;
            display: flex; align-items: center; justify-content: center;
        }
        .machine-props-btn:hover { background: rgba(0,210,255,0.25); border-color: var(--primary); }
        
        .machine-name-container { width: 100%; }
        .machine-name {
            display: block; white-space: nowrap; overflow: hidden;
            text-overflow: ellipsis; font-weight: bold; font-size: 0.9rem;
            width: 100%;
        }

        .machine-game-container {
            width: 100%; background: rgba(0,210,255,0.08); border-radius: 8px;
            border: 1px solid rgba(0,210,255,0.2); box-sizing: border-box;
            padding: 5px 8px; display: flex; flex-direction: column; gap: 2px;
        }
        .machine-game-system {
            font-size: 0.65rem; color: rgba(0,210,255,0.7); text-transform: uppercase;
            letter-spacing: 1px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
        }
        .machine-game-title {
            font-size: 0.75rem; color: var(--primary); font-weight: bold;
            white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
        }
        .machine-game-title::before { content: ""🎮 ""; }

        /* Game Info Card */
        .game-card {
            background: linear-gradient(135deg, rgba(0, 210, 255, 0.1), rgba(58, 123, 213, 0.1));
            backdrop-filter: blur(15px);
            border: 1px solid rgba(255, 255, 255, 0.1);
            border-radius: 20px;
            padding: 20px;
            margin-bottom: 20px;
            display: flex;
            align-items: center;
            gap: 20px;
            text-align: left;
            position: relative;
            overflow: hidden;
            box-shadow: 0 10px 30px rgba(0,0,0,0.3);
        }

        .game-card::before {
            content: """";
            position: absolute;
            top: 0; left: 0; width: 4px; height: 100%;
            background: var(--primary);
        }

        .game-icon-large { font-size: 2.5rem; filter: drop-shadow(0 0 10px var(--primary)); }
        .game-details { flex: 1; }
        .game-system { font-size: 0.7rem; font-weight: 800; text-transform: uppercase; color: var(--primary); letter-spacing: 2px; }
        .game-title { font-size: 1.3rem; font-weight: bold; margin: 4px 0; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
        .game-exe { font-size: 0.7rem; opacity: 0.5; font-family: monospace; }
        .game-duration { font-size: 0.8rem; font-weight: bold; color: var(--primary); margin-top: 5px; display: flex; align-items: center; gap: 5px; }
        
        /* History Modal Styles */
        .modal {
            display: none; position: fixed; z-index: 1000; left: 0; top: 0; width: 100%; height: 100%;
            background-color: rgba(0,0,0,0.8); backdrop-filter: blur(10px);
        }
        .modal-content {
            background: var(--card); margin: 5% auto; padding: 20px; border: 1px solid rgba(255,255,255,0.1);
            width: 90%; max-width: 800px; border-radius: 20px; max-height: 80vh; overflow-y: auto;
            box-shadow: 0 20px 50px rgba(0,0,0,0.5); position: relative;
        }
        .close { color: #aaa; float: right; font-size: 28px; font-weight: bold; cursor: pointer; }
        .history-table { width: 100%; border-collapse: collapse; margin-top: 20px; font-size: 0.85rem; }
        .history-table th, .history-table td { text-align: left; padding: 12px; border-bottom: 1px solid rgba(255,255,255,0.05); }
        .history-table th { color: var(--primary); text-transform: uppercase; font-size: 0.7rem; letter-spacing: 1px; }
        .history-system { font-weight: bold; color: var(--secondary); font-size: 0.7rem; }
        
        .status-dot {
            width: 10px; height: 10px; border-radius: 50%; display: inline-block; 
        }
        .dot-online { background: var(--success); box-shadow: 0 0 10px var(--success); }
        .dot-offline { background: var(--danger); }

        .btn-home {
            display: inline-block;
            margin-bottom: 20px;
            padding: 10px 15px; 
            background: rgba(255,255,255,0.1);
            border-radius: 10px; 
            border: 1px solid rgba(255,255,255,0.2); 
            cursor: pointer; 
            color: white;
            z-index: 100;
        }

        @keyframes fadeIn {
            from { opacity: 0; transform: translateY(20px); }
            to { opacity: 1; transform: translateY(0); }
        }

        header {
            text-align: center;
            margin-bottom: 30px;
            position: relative;
        }

        .lang-switch {
            display: flex;
            gap: 10px;
            justify-content: center;
            margin-top: 10px;
        }

        .flag {
            font-size: 0.9rem;
            font-weight: bold;
            cursor: pointer;
            filter: grayscale(0.6);
            transition: all 0.3s;
            background: rgba(255,255,255,0.1);
            padding: 5px 10px;
            border-radius: 8px;
            display: flex;
            align-items: center;
            gap: 5px;
        }

        .flag.active {
            filter: grayscale(0);
            transform: scale(1.1);
            background: rgba(255,255,255,0.2);
            border: 1px solid var(--primary);
        }

        h1 {
            font-size: 1.8rem;
            background: linear-gradient(to right, var(--primary), var(--secondary));
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            margin-bottom: 5px;
        }
        .header {
            text-align: center;
            margin-bottom: 5px;
        }

        .machine-info {
            text-align: center;
            font-size: 0.9rem;
            opacity: 0.6;
            margin-bottom: 15px;
        }
        
        .status-card {
            background: var(--card);
            backdrop-filter: blur(10px);
            border: 1px solid rgba(255, 255, 255, 0.1);
            border-radius: 20px;
            padding: 30px;
            text-align: center;
            margin-bottom: 20px;
            box-shadow: 0 10px 30px rgba(0,0,0,0.5);
        }

        .status-label { font-size: 0.9rem; text-transform: uppercase; letter-spacing: 2px; opacity: 0.7; margin-bottom: 10px; }
        
        .time-display {
            font-size: 4rem;
            font-weight: 800;
            margin: 10px 0;
            font-variant-numeric: tabular-nums;
            text-shadow: 0 0 20px var(--primary);
        }

        .credits-badge {
            display: inline-block;
            padding: 8px 20px;
            background: rgba(255,255,255,0.1);
            border-radius: 50px;
            font-weight: 600;
            margin-top: 10px;
        }

        .freeplay-mode { color: var(--success); font-weight: bold; }

        .actions-grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 15px;
        }

        button {
            border: none;
            border-radius: 15px;
            padding: 20px;
            font-size: 1rem;
            font-weight: 600;
            color: white;
            cursor: pointer;
            transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            gap: 10px;
            background: rgba(255,255,255,0.05);
            border: 1px solid rgba(255,255,255,0.1);
        }

        button:active { transform: scale(0.95); }
        button:hover { background: rgba(255,255,255,0.1); border-color: var(--primary); }

        .btn-primary { background: linear-gradient(135deg, var(--primary), var(--secondary)); box-shadow: 0 5px 15px rgba(0, 210, 255, 0.3); }
        .btn-danger { background: linear-gradient(135deg, #ff416c, #ff4b2b); box-shadow: 0 5px 15px rgba(255, 65, 108, 0.3); }
        .btn-success { background: linear-gradient(135deg, #00b09b, #96c93d); box-shadow: 0 5px 15px rgba(0, 176, 155, 0.3); }

        .btn-wide { grid-column: span 2; }

        .btn-icon { font-size: 1.5rem; }
        .message-box {
            background: var(--card);
            padding: 20px;
            border-radius: 15px;
            margin-top: 20px;
            border: 1px solid rgba(255,255,255,0.1);
        }

        .message-box h3 { font-size: 0.9rem; opacity: 0.7; margin-bottom: 10px; text-transform: uppercase; }

        .msg-input-group { display: flex; gap: 10px; margin-bottom: 10px; }
        .msg-input-group input { margin: 0; text-align: left; padding: 10px; font-size: 1rem; flex: 1; }
        .msg-input-group select { 
            background: rgba(255,255,255,0.1); 
            border: 1px solid rgba(255,255,255,0.2); 
            border-radius: 10px; 
            color: white; 
            padding: 0 10px;
            outline: none;
        }

        /* Login Overlay */
        #loginOverlay {
            position: fixed;
            top: 0; left: 0; right: 0; bottom: 0;
            background: var(--bg);
            z-index: 1000;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 20px;
            display: none;
        }

        .login-card {
            background: var(--card);
            padding: 40px;
            border-radius: 20px;
            width: 100%;
            max-width: 350px;
            text-align: center;
            border: 1px solid rgba(255,255,255,0.1);
        }

        input {
            width: 100%;
            padding: 15px;
            background: rgba(255,255,255,0.1);
            border: 1px solid rgba(255,255,255,0.2);
            border-radius: 10px;
            color: white;
            margin: 20px 0;
            text-align: center;
            font-size: 1.2rem;
            outline: none;
        }

        input:focus { border-color: var(--primary); }

        .setting-group {
            display: grid;
            grid-template-columns: 1fr auto;
            gap: 10px;
            margin-top: 10px;
        }


        .setting-group input {
            margin: 0;
            padding: 10px;
            font-size: 1rem;
        }

        .btn-small {
            padding: 10px 20px;
            font-size: 0.8rem;
        }

        @media (max-width: 400px) {
            h1 { font-size: 1.5rem; }
            .time-display { font-size: 3rem; }
            .lang-switch { position: static; justify-content: center; margin-bottom: 15px; }
        }
        .sr-only {
            position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px;
            overflow: hidden; clip: rect(0,0,0,0); border: 0;
        }
    </style>
</head>
<body>
    <div id=""home-view"" class=""container"" style=""display: block; max-width: 900px;"">
        <header>
            <div class=""header"">
                <h1 id=""networkTitle"">BatRun Network</h1>
                <p id=""networkDesc"" style=""opacity:0.6; margin-top:5px;"">Select a machine</p>
                <div style=""margin-top: 15px; display: flex; gap: 10px; justify-content: center; flex-wrap: wrap;"">
                    <button onclick=""addManualMachine()"" class=""btn-primary btn-small"" style=""display: inline-flex; flex-direction: row; padding: 10px 15px; width: auto; margin: 0;"" id=""btnAddMachine"">
                        ➕ <span id=""labelAddMachine"">AJOUTER UNE MACHINE (IP)</span>
                    </button>
                    <button onclick=""showLauncher()"" class=""btn-success btn-small"" style=""display: inline-flex; flex-direction: row; padding: 10px 15px; width: auto; margin: 0;"" id=""btnLauncher"">
                        🎮 <span id=""labelLauncher"">LANCEUR DE JEUX</span>
                    </button>
                    <button onclick=""showUsers()"" class=""btn-primary btn-small"" style=""display: inline-flex; flex-direction: row; padding: 10px 15px; width: auto; margin: 0; background: #f39c12; border-color: #f39c12;"" id=""btnUsers"">
                        👤 <span id=""labelUsers"">UTILISATEURS</span>
                        <span id=""pendingUsersBadge"" style=""display:none; background:var(--danger); color:white; border-radius:10px; padding:2px 6px; font-size:0.7em; margin-left:5px; font-weight:bold;"">0</span>
                    </button>
                    <button onclick=""showSecurity()"" class=""btn-danger btn-small"" style=""display: inline-flex; flex-direction: row; padding: 10px 15px; width: auto; margin: 0;"" id=""btnSecurity"">
                        🛡️ <span id=""labelSecurity"">SÉCURITÉ</span>
                    </button>
                </div>
                <p id=""network-count"" style=""opacity:0.8; font-weight:bold; margin-top:10px;""></p>
            </div>
            <div class=""lang-switch"">
                <span class=""flag"" id=""flag-fr-home"" onclick=""setLanguage('fr')""><img src=""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 3 2'%3E%3Crect width='3' height='2' fill='%23ED2939'/%3E%3Crect width='2' height='2' fill='%23fff'/%3E%3Crect width='1' height='2' fill='%23002395'/%3E%3C/svg%3E"" style=""width:18px; height:12px; border-radius:2px;"" alt=""FR""> FR</span>
                <span class=""flag"" id=""flag-en-home"" onclick=""setLanguage('en')""><img src=""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 60 30'%3E%3CclipPath id='t'%3E%3Cpath d='M30,15h30v15zv15H0zH0V0zV0h30z'/%3E%3C/clipPath%3E%3Cpath d='M0,0v30h60V0z' fill='%23012169'/%3E%3Cpath d='M0,0 60,30M60,0 0,30' stroke='%23fff' stroke-width='6'/%3E%3Cpath d='M0,0 60,30M60,0 0,30' clip-path='url(%23t)' stroke='%23C8102E' stroke-width='4'/%3E%3Cpath d='M30,0v30M0,15h60' stroke='%23fff' stroke-width='10'/%3E%3Cpath d='M30,0v30M0,15h60' stroke='%23C8102E' stroke-width='6'/%3E%3C/svg%3E"" style=""width:18px; height:12px; border-radius:2px;"" alt=""EN""> EN</span>
            </div>
        </header>
        <div id=""network-grid"" class=""network-grid""></div>
    </div>

    <div class=""container"" id=""launcher-view"" style=""display:none"">
        <header>
            <button class=""btn-home"" onclick=""showHome()"">🏠 Home</button>
            <div class=""header"">
                <h1 id=""launcherTitle"">Explorateur de Jeux</h1>
            </div>
        </header>

        <div id=""esStatusWarning"" class=""es-warning"">
             ⚠️ <strong id=""esWarnTitle"">API EmulationStation non détectée</strong><br>
             <span id=""esWarnDesc"">Recherche désactivée. Activez ""Web Server"" dans les options RetroBat.</span>
        </div>

        <div class=""machine-selector"" id=""machineLaunchList""></div>

        <!-- EN: Now Playing Banner / FR: Bandeau jeu en cours -->
        <div id=""launcherCurrentGameBanner"" style=""display:none; background: linear-gradient(135deg, rgba(31,64,55,0.9), rgba(153,242,200,0.4)); padding: 15px; border-radius: 12px; margin-bottom: 20px; flex-direction: column; gap: 0; border: 1px solid rgba(255,255,255,0.15); box-shadow: 0 4px 15px rgba(0,0,0,0.3);"">
            <div id=""bannerSummary"" style=""display:flex; align-items:center; width:100%; justify-content:space-between; gap:15px;"">
                <div style=""flex: 1; min-width: 0;"">
                    <div style=""font-size: 0.65rem; opacity: 0.7; font-weight: 800; margin-bottom: 2px; text-transform:uppercase; letter-spacing:1px;"" id=""launcherBannerSystem"">SYSTEM</div>
                    <div style=""font-size: 1rem; font-weight: 900; color:white; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;"" id=""launcherBannerGame"">GAME NAME</div>
                </div>
                <div style=""display:flex; align-items:center; gap:10px;"">
                    <button id=""btnToggleBanner"" onclick=""toggleLauncherBannerDetails()"" class=""banner-toggle-btn"" style=""display:none;"" title=""Voir détails"">▼</button>
                    <button onclick=""stopCurrentGameFromWeb()"" class=""btn-danger"" style=""margin:0; padding:8px 12px; font-weight:bold; font-size:0.75rem; border:none; border-radius:8px; cursor:pointer;"" id=""btnStopGameLaunch"">🛑 STOP</button>
                </div>
            </div>
            <div id=""bannerDetailsList"" class=""banner-details""></div>
        </div>

        <div class=""search-container sticky"" id=""launcherSearchContainer"">

            <div class=""search-filter-group"">
                <select id=""systemFilter"" onchange=""searchGames()"">
                    <option value="""">TOUS LES SYSTÈMES</option>
                </select>
                
                <select id=""sortFilter"" onchange=""searchGames()"">
                    <option value=""system"">Système (A-Z)</option>
                    <option value=""game"">Tri : Nom (A-Z)</option>
                </select>
            </div>

            <div class=""search-main-group"">
                <div class=""search-input-group"">
                    <input type=""search""
                           id=""gameSearchInput""
                           name=""search_q""
                           autocomplete=""one-time-code""
                           readonly
                           onfocus=""this.removeAttribute('readonly');""
                           onblur=""this.setAttribute('readonly', true);""
                           placeholder=""Rechercher un jeu...""
                           oninput=""searchGamesDebounced()""
                           style=""flex:1;"">
                </div>
                
                <button onclick=""searchGames()"" class=""btn-primary"" id=""btnSearch"" style=""padding: 0; width: 45px; height: 45px; margin: 0; min-height: 45px; border-radius: 10px; display:flex; align-items:center; justify-content:center; color: white;"">
                    <svg width=""22"" height=""22"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2.5"" stroke-linecap=""round"" stroke-linejoin=""round""><circle cx=""11"" cy=""11"" r=""8""></circle><line x1=""21"" y1=""21"" x2=""16.65"" y2=""16.65""></line></svg>
                </button>
                <button onclick=""reloadSelectedEs()"" class=""btn-primary"" id=""btnReloadSelectedES"" title=""Recharger Jeux"" style=""padding: 0; width: 45px; height: 45px; margin: 0 0 0 5px; min-height: 45px; border-radius: 10px; display:flex; align-items:center; justify-content:center; color: white;"">
                    <svg width=""22"" height=""22"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2.5"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M21.5 2v6h-6M2.5 22v-6h6M2 11.5a10 10 0 0 1 18.8-4.3M22 12.5a10 10 0 0 1-18.8 4.2""/></svg>
                </button>
            </div>
        </div>

        <div class=""search-results"" id=""searchResults""></div>
    </div>

    <!-- Game Detail Modal -->
    <div id=""gameDetailsModal"" class=""modal"">
        <div class=""modal-content"" style=""max-width: 600px;"">
            <span class=""close"" onclick=""closeGameDetails()"">&times;</span>
            <div id=""gameDetailContent"" style=""text-align: left;"">
                <h2 id=""mdTitle"" style=""color: var(--primary); margin-bottom: 5px;"">Title</h2>
                <div id=""mdSystem"" style=""font-size: 0.75rem; color: rgba(255,255,255,0.5); text-transform: uppercase; margin-bottom: 15px;"">System</div>
                
                <div style=""display: flex; gap: 20px; flex-wrap: wrap;"">
                    <div style=""flex: 1; min-width: 200px;"">
                        <img id=""mdImage"" src="""" style=""width: 100%; border-radius: 12px; border: 1px solid rgba(255,255,255,0.1); margin-bottom: 15px;"">
                        <video id=""mdVideo"" src="""" controls style=""width: 100%; border-radius: 12px; display: none;""></video>
                    </div>
                    <div style=""flex: 1.5; min-width: 250px;"">
                        <p id=""mdDesc"" style=""font-size: 0.9rem; line-height: 1.5; opacity: 0.8; margin-bottom: 20px; max-height: 250px; overflow-y: auto;""></p>
                        
                        <div id=""mdAvailableMachines"" style=""margin-bottom: 20px;"">
                            <h4 id=""mdAvailLabel"" style=""font-size: 0.8rem; margin-bottom: 10px; opacity: 0.6;"">DISPONIBILITÉ :</h4>
                            <div id=""availList"" style=""display:flex; flex-direction:column; gap:8px;""></div>
                        </div>

                        <button onclick=""launchSelectedGame()"" class=""btn-success"" style=""width: 100%; padding: 15px; font-weight: bold; border-radius: 12px;"" id=""btnLaunchNow"">
                            🚀 LANCER SUR LES BORNES
                        </button>
                        <button id=""btnManual"" onclick="""" class=""btn"" style=""display:none; width:100%; margin-top:10px; background:rgba(255,255,255,0.1); padding:12px; border-radius:12px;"">📖 MANUEL</button>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <div id=""loginOverlay"" style=""display:none"">
        <form class=""login-card"" method=""POST"" action=""/admin"" onsubmit=""event.preventDefault(); login(); return false;"">
            <h2 id=""loginTitle"">Accès Opérateur</h2>
            <p id=""loginDesc"" style=""opacity: 0.7; margin-top: 10px;"">Veuillez entrer le mot de passe machine.</p>
            <label for=""passInput"" class=""sr-only"">Mot de passe</label>
            <input type=""password"" id=""passInput"" name=""password"" autocomplete=""current-password"" placeholder=""••••••"">
            <button type=""submit"" class=""btn-primary"" style=""width: 100%"" id=""btnLogin"">SE CONNECTER</button>
        </form>
    </div>

    <div class=""container"" id=""mainContent"" style=""display:none"">
        <header>
            <button class=""btn-home"" onclick=""showHome()"">🏠 Home</button>
            <div class=""header"">
                <h1 id=""mainTitle"">BatRun Operator Dashboard</h1>
                <span id=""operator-badge"" class=""status-badge badge-operator"">MODE OPERATEUR</span>
            </div>

            <div class=""machine-info"">
                <span id=""machineName"">MACHINE: --</span>
                <div class=""lang-switch"">
                    <span class=""flag"" id=""flag-fr"" onclick=""setLanguage('fr')""><img src=""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 3 2'%3E%3Crect width='3' height='2' fill='%23ED2939'/%3E%3Crect width='2' height='2' fill='%23fff'/%3E%3Crect width='1' height='2' fill='%23002395'/%3E%3C/svg%3E"" style=""width:18px; height:12px; border-radius:2px;"" alt=""FR""> FR</span>
                    <span class=""flag"" id=""flag-en"" onclick=""setLanguage('en')""><img src=""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 60 30'%3E%3CclipPath id='t'%3E%3Cpath d='M30,15h30v15zv15H0zH0V0zV0h30z'/%3E%3C/clipPath%3E%3Cpath d='M0,0v30h60V0z' fill='%23012169'/%3E%3Cpath d='M0,0 60,30M60,0 0,30' stroke='%23fff' stroke-width='6'/%3E%3Cpath d='M0,0 60,30M60,0 0,30' clip-path='url(%23t)' stroke='%23C8102E' stroke-width='4'/%3E%3Cpath d='M30,0v30M0,15h60' stroke='%23fff' stroke-width='10'/%3E%3Cpath d='M30,0v30M0,15h60' stroke='%23C8102E' stroke-width='6'/%3E%3C/svg%3E"" style=""width:18px; height:12px; border-radius:2px;"" alt=""EN""> EN</span>
                </div>
            </div>
        </header>

        <div id=""gameInfoContainer"" class=""game-card"" style=""display:none"">
            <div class=""game-icon-large"">🎮</div>
            <div class=""game-details"">
                <div id=""gameSystem"" class=""game-system"">SYSTEM</div>
                <div id=""gameTitle"" class=""game-title"">LOADING GAME...</div>
                <div id=""gameExe"" class=""game-exe"">exe: --</div>
                <div id=""gameDuration"" class=""game-duration"">
                    <span>⏱️</span> <span id=""gameDurationText"">00:00</span>
                </div>
            </div>
        </div>

        <div style=""text-align: center; margin: 15px 0;"">
            <button class=""btn-home"" onclick=""toggleHistory()"" id=""btnHistory"">📜 Historique des parties</button>
        </div>


        <div class=""status-card"">
            <div class=""status-label"" id=""statusLabel"">TEMPS RESTANT</div>
            <div class=""time-display"" id=""timeDisplay"">--:--</div>
            <div class=""status-badge-container"" style=""margin-top: 10px;"">
                <span class=""credits-badge"" id=""creditsDisplay"">0 CRÉDITS</span>
                <span id=""freeplay-badge"" class=""status-badge badge-freeplay"">FREEPLAY</span>
            </div>
            <button onclick=""confirmOperator()"" class=""btn-primary btn-small"" style=""margin: 20px auto 0 auto; width: auto; padding: 10px 20px;"" id=""btnLocalOperator"">
                🛠️ MODE OPERATEUR
            </button>
            <div style=""margin-top: 15px; font-size: 0.8rem; display: flex; align-items: center; justify-content: center; gap: 8px;"">
                <input type=""checkbox"" id=""chkHideOpButtons"" style=""width: 16px; height: 16px; margin: 0;"" onclick=""toggleHideOpButtons()"">
                <label for=""chkHideOpButtons"" id=""labelHideOpButtons"" style=""cursor: pointer; opacity: 0.8;"">Masquer les boutons flottants (LOCK/BR)</label>
            </div>
        </div>

        <div class=""actions-grid"">
            <button onclick=""performAction('add_credit', {count: 1})"" class=""btn-success"">
                <span class=""btn-icon"">🪙</span>
                <span id=""labelAdd1"">+1 CRÉDIT</span>
            </button>
            <button onclick=""performAction('add_credit', {count: 10})"" class=""btn-primary"">
                <span class=""btn-icon"">💰</span>
                <span id=""labelAdd10"">+10 CRÉDITS</span>
            </button>
            <button onclick=""performAction('remove_credit')"" style=""opacity: 0.8"">
                <span class=""btn-icon"">➖</span>
                <span id=""labelRemove1"">-1 CRÉDIT</span>
            </button>
            <button onclick=""confirmFreePlay()"" id=""btnFreePlay"" style=""border-color: rgba(255,255,255,0.1)"">
                <span class=""btn-icon"">♾️</span>
                <span id=""labelFreePlay"">FREE PLAY</span>
            </button>
            <button onclick=""confirmLock()"" class=""btn-danger btn-wide"">
                <span class=""btn-icon"">🔒</span>
                <span id=""labelForceLock"">FORCE LOCK (GAME OVER)</span>
            </button>
        </div>

        <div class=""message-box"">
            <h3 id=""titleSession"">Paramètres de Session Directe</h3>
            <div class=""setting-group"">
                <input type=""number"" id=""sessionMins"" placeholder=""Minutes"" value=""60"">
                <button onclick=""setDuration()"" class=""btn-success btn-small"" id=""btnSetTime"">REGLER LE TEMPS</button>
            </div>
            <p style=""font-size: 0.7rem; opacity: 0.5; margin-top: 5px;"" id=""descSession"">Écrase le temps actuel sans toucher aux crédits.</p>
        </div>

        <div class=""message-box"">
            <h3 id=""titleCreditParams"">Paramètres des Crédits</h3>
            <div class=""setting-group"">
                <input type=""number"" id=""mpcInput"" placeholder=""Min"">
                <button onclick=""setMPC()"" class=""btn-primary btn-small"" id=""btnSaveMPC"">ENREGISTRER</button>
            </div>
            <p style=""font-size: 0.7rem; opacity: 0.5; margin-top: 5px;"" id=""descCreditParams"">Définit le temps ajouté par chaque crédit.</p>
        </div>

        <div class=""message-box"">
            <h3 id=""titleCloud"">Mode / Cloud (Accès Externe)</h3>
            <div class=""setting-group"">
                <input type=""text"" id=""publicIpInput"" placeholder=""IP Publique (ex: 82.10.x.x)"">
                <button onclick=""setPublicIp()"" class=""btn-primary btn-small"" id=""btnSavePublicIp"">ENREGISTRER</button>
            </div>
            <p style=""font-size: 0.7rem; opacity: 0.5; margin-top: 5px;"" id=""descCloud"">IP utilisée par les clients externes. Laissez vide pour utiliser la découverte STUN (Google/Cloudflare).</p>
        </div>

        <div class=""message-box"">
            <h3 id=""titleMessage"">Envoyer un message à l'écran</h3>
            <div class=""msg-input-group"">
                <input type=""text"" id=""opMessage"" autocomplete=""off"" placeholder=""Ex: Maintenance..."">
                <select id=""msgDuration"">
                    <option value=""3"">3s</option>
                    <option value=""5"" selected>5s</option>
                    <option value=""10"">10s</option>
                    <option value=""30"">30s</option>
                </select>
            </div>
            <button onclick=""sendOpMessage()"" style=""width: 100%; padding: 12px;"" class=""btn-primary"" id=""btnSendMessage"">
                ENVOYER LE MESSAGE
            </button>
        </div>


    </div>

    <!-- Properties Modal (Shared) -->
    <div id=""propModal"" class=""modal"">
        <div class=""modal-content"" style=""max-width: 500px;"">
            <span class=""close"" onclick=""closeProperties()"">&times;</span>
            <h2 id=""propTitle"" style=""margin-bottom: 20px; color: var(--primary);"">Propriétés de la machine</h2>
            <div style=""margin-bottom: 15px; font-size: 0.9rem;"">
                <strong id=""propMachineLabel"">Machine :</strong> <span id=""propName"" style=""font-weight: bold;""></span>
            </div>
            <div style=""margin-bottom: 15px; font-size: 0.9rem;"">
                <strong id=""propMacLabel"">MAC Address :</strong> <span id=""propMac"" style=""font-family: monospace; opacity: 0.8;""></span>
            </div>
            <div style=""margin-bottom: 20px; font-size: 0.9rem;"">
                <strong id=""propIpHistoryLabel"">IP History :</strong>
                <ul id=""propIpHistory"" style=""list-style: none; padding-left: 10px; margin-top: 5px; opacity: 0.8; font-family: monospace;""></ul>
            </div>
            <button onclick=""removeMachine()"" class=""btn-danger"" style=""width: 100%; padding: 12px;"" id=""btnRemoveMachine"">
                🗑️ <span id=""propDeleteLabel"">SUPPRIMER DE LA LISTE</span>
            </button>
            <button onclick=""reloadEs()"" class=""btn-primary"" style=""width: 100%; padding: 12px; margin-top: 10px; background: rgba(0,210,255,0.2);"" id=""btnReloadES"">
                🔄 <span id=""labelReloadES"">RECHARGER JEUX (ES)</span>
            </button>
        </div>
    </div>

    <!-- Reload Confirmation Modal (Custom) -->
    <div id=""reloadConfirmModal"" class=""modal"">
        <div class=""modal-content"" style=""max-width: 500px;"">
            <span class=""close"" onclick=""closeReloadConfirm()"">&times;</span>
            <h2 id=""confirmReloadTitle"" style=""margin-bottom: 20px; color: var(--primary);"">Confirmation de Rechargement</h2>
            <p id=""confirmReloadES"" style=""margin-bottom: 20px; opacity: 0.9;"">Voulez-vous vraiment recharger la liste des jeux sur ces machines ?</p>
            <div style=""max-height: 200px; overflow-y: auto; background: rgba(0,0,0,0.2); border-radius: 10px; padding: 10px; margin-bottom: 20px;"">
                <ul id=""reloadMachinesList"" style=""list-style: none; padding: 0; margin: 0; font-family: monospace; font-size: 0.9rem;""></ul>
            </div>
            <div style=""display: flex; gap: 10px;"">
                <button onclick=""confirmReloadES()"" class=""btn-primary"" style=""flex: 1; padding: 12px;"" id=""btnConfirmReload"">CONFIRMER</button>
                <button onclick=""closeReloadConfirm()"" class=""btn-secondary"" style=""flex: 1; padding: 12px;"" id=""btnCancelReload"">ANNULER</button>
            </div>
        </div>
    </div>

    <!-- Users View -->
    <div id=""users-view"" class=""container"" style=""display:none; max-width: 900px;"">
        <header>
            <button class=""btn-home"" onclick=""showHome()"">🏠 Home</button>
            <div class=""header"">
                <h1 id=""usersTitle"">Gestion des Utilisateurs</h1>
                <p style=""opacity:0.6; margin-top:5px;"" id=""usersSub"">Approbation des accès publics</p>
            </div>
        </header>
        <div class=""status-card"" style=""padding: 15px; overflow-x: auto;"">
            <table class=""history-table"" style=""width: 100%;"" id=""usersTable"">
                <thead>
                    <tr>
                        <th id=""thUsername"">USERNAME</th>
                        <th style=""min-width: 80px;"" id=""thStatus"">STATUT</th>
                        <th id=""thDate"">DATE</th>
                        <th style=""text-align: right;"" id=""thActions"">ACTIONS</th>
                    </tr>
                </thead>
                <tbody id=""usersBody"">
                    <tr><td colspan=""4"">Chargement...</td></tr>
                </tbody>
            </table>
        </div>
    </div>

    <!-- History Modal -->
    <div id=""historyModal"" class=""modal"">
        <div class=""modal-content"">
            <span class=""close"" onclick=""toggleHistory()"">&times;</span>
            <h2 id=""historyTitle"">Dernières sessions de jeu</h2>
            <div style=""overflow-x:auto"">
                <table class=""history-table"">
                    <thead>
                        <tr>
                            <th id=""thSystem"">SYSTÈME</th>
                            <th id=""thTitle"">JEU</th>
                            <th id=""thDuration"">DURÉE</th>
                            <th id=""thDate"">DATE</th>
                        </tr>
                    </thead>
                    <tbody id=""historyBody""></tbody>
                </table>
            </div>
            <div id=""historyPagination"" style=""text-align:center; margin-top:20px; display:none;"">
                <button onclick=""renderMoreHistory()"" class=""btn-secondary"" id=""btnLoadMore"" style=""width:100%; padding:10px;"">Voir plus</button>
            </div>
        </div>
    </div>

    <!-- Security Modal -->
    <div id=""securityModal"" class=""modal"">
        <div class=""modal-content"" style=""max-width: 900px;"">
            <span class=""close"" onclick=""toggleSecurity()"">&times;</span>
            <h2 id=""securityTitle"">Sécurité & Journal des accès</h2>
            
            <div style=""display: flex; gap: 10px; margin-bottom: 20px; align-items: center; background: rgba(0,0,0,0.2); padding: 10px; border-radius: 8px;"">
                <label for=""logDateSelect"" style=""font-size: 0.8rem; opacity: 0.8;"" id=""labelLogDate"">Historique :</label>
                <select id=""logDateSelect"" onchange=""loadSecurityLogs(this.value)"" style=""background: #161625; color: white; border: 1px solid rgba(255,255,255,0.2); border-radius: 6px; padding: 6px 12px; font-size: 0.85rem; cursor: pointer; outline: none;"">
                    <option value="""" id=""optLiveLogs"">🔴 TEMPS RÉEL (Session active)</option>
                </select>
                <button onclick=""refreshSecurity()"" class=""btn-secondary"" style=""padding: 6px; width: auto; margin: 0; min-width: unset; opacity: 0.7;"" title=""Refresh everything"">🔄</button>
            </div>

            <div style=""display:grid; grid-template-columns: 1fr 1fr; gap:20px;"">
                <div class=""card"" style=""background:rgba(255,0,0,0.05); padding:15px; border-radius:12px; border:1px solid rgba(255,0,0,0.1);"">
                    <h3 style=""color:var(--danger); font-size:0.9rem; margin-bottom:10px;"">🚫 IPS BANNIES (24H)</h3>
                    <div id=""blockedIpList"" style=""max-height:300px; overflow-y:auto; font-size:0.85rem;""></div>
                </div>
                <div class=""card"" style=""background:rgba(0,210,255,0.05); padding:15px; border-radius:12px; border:1px solid rgba(0,210,255,0.1);"">
                    <h3 style=""color:var(--primary); font-size:0.9rem; margin-bottom:10px;"">🔍 DERNIÈRES TENTATIVES</h3>
                    <div id=""securityLogList"" style=""max-height:300px; overflow-y:auto; font-size:0.85rem;""></div>
                </div>
            </div>
        </div>
    </div>

    <script>
        const translations = {
            fr: {
                loginTitle: ""Accès Opérateur"",
                loginDesc: ""Veuillez entrer le mot de passe machine."",
                btnLogin: ""SE CONNECTER"",
                mainTitle: ""BatRun Operator Dashboard"",
                statusRemaining: ""TEMPS RESTANT"",
                statusCoin: ""INSERT COIN"",
                statusFreeActive: ""MODE FREE PLAY ACTIF"",
                credits: "" CRÉDITS"",
                labelAdd1: ""+1 CRÉDIT"",
                labelAdd10: ""+10 CRÉDITS"",
                labelRemove1: ""-1 CRÉDIT"",
                labelFreePlay: ""FREE PLAY"",
                labelForceLock: ""FORCE LOCK (GAME OVER)"",
                titleSession: ""Paramètres de Session Directe"",
                descSession: ""Écrase le temps actuel sans toucher aux crédits."",
                btnSetTime: ""REGLER LE TEMPS"",
                titleCreditParams: ""Paramètres des Crédits"",
                descCreditParams: ""Définit le temps ajouté par chaque crédit."",
                titleCloud: ""Mode / Cloud (Accès Externe)"",
                descCloud: ""IP utilisée par les clients externes. Laissez vide pour utiliser la découverte STUN (Google/Cloudflare)."",
                btnSavePublicIp: ""ENREGISTRER"",
                btnSaveMPC: ""ENREGISTRER"",
                titleMessage: ""Envoyer un message à l'écran"",
                btnSendMessage: ""ENVOYER LE MESSAGE"",
                msgPlaceholder: ""Ex: Maintenance dans 5 min..."",
                timeMins: ""MIN"",
                btnLocalOperator: ""🛠️ MODE OPERATEUR"",
                confirmLock: ""Voulez-vous vraiment verrouiller la session ? Cela provoquera un GAME OVER immédiat."",
                confirmFreePlay: ""Voulez-vous basculer le mode FREE PLAY ? Cela affecte le décompte du temps."",
                confirmOperator: ""Voulez-vous basculer le mode OPÉRATEUR sur cette borne locale ?"",
                passWrong: ""Mot de passe incorrect"",
                apiError: ""Erreur lors de l'action"",
                networkTitle: ""Réseau BatRun"",
                networkDesc: ""Sélectionnez une borne pour la gérer"",
                gameSystem: ""SYSTÈME"",
                operatorBadge: ""MODE OPÉRATEUR"",
                historyBtn: ""📜 Historique des parties"",
                historyTitle: ""Dernières sessions de jeu"",
                colSystem: ""SYSTÈME"",
                colTitle: ""JEU"",
                colDuration: ""DURÉE"",
                colDate: ""DATE"",
                labelSecurity: ""SÉCURITÉ"",
                securityTitle: ""Sécurité & Journal des accès"",
                noBlockedIps: ""Aucune IP bannie"",
                noSecurityLogs: ""Aucun log disponible"",
                labelLogDate: ""Historique :"",
                optLiveLogs: ""🔴 TEMPS RÉEL (Session active)"",
                gameUnknown: ""RETROBAT / MENU"",
                propTitle: ""Propriétés de la machine"",
                propMachineLabel: ""Machine :"",
                propMacLabel: ""Adresse MAC :"",
                propIpHistoryLabel: ""Historique des IPs :"",
                propDeleteLabel: ""SUPPRIMER DE LA LISTE"",
                noIpKnown: ""Aucune IP connue"",
                labelAddMachine: ""AJOUTER UNE MACHINE (IP)"",
                promptAddIp: ""Entrez l'adresse IP de la borne à ajouter :"",
                labelHideOpButtons: ""Masquer les boutons flottants (LOCK/BR)"",
                labelUsers: ""UTILISATEURS"",
                usersTitle: ""Gestion des Utilisateurs"",
                usersSub: ""Approbation des accès publics"",
                thUsername: ""NOM D'UTILISATEUR"",
                thStatus: ""STATUT"",
                thActions: ""ACTIONS"",
                statusApproved: ""Approuvé"",
                statusRejected: ""Rejeté"",
                statusPending: ""En attente"",
                labelLauncher: ""LANCEUR DE JEUX"",
                launcherTitle: ""Explorateur de Jeux"",
                esWarnTitle: ""API EmulationStation non détectée"",
                esWarnDesc: ""La recherche est désactivée car le serveur Web d'ES ne répond pas. Activez-le dans RetroBat (Options Développement)."",
                gameSearchPlaceholder: ""Rechercher un jeu..."",
                mdAvailLabel: ""DISPONIBILITÉ :"",
                btnLaunchNow: ""🚀 LANCER SUR LES SÉLECTIONNÉS"",
                availOk: ""Présent"",
                availMissing: ""Absent"",
                playingBadge: ""EN COURS"",
                launchingBadge: ""LANCEMENT..."",
                btnStopGame: ""🛑 ARRETER LE JEU"",
                allSystems: ""TOUS LES SYSTÈMES"",
                sortSystem: ""Tri : Système (A-Z)"",
                sortName: ""Tri : Nom (A-Z)"",
                labelReloadES: ""RECHARGER JEUX (ES)"",
                confirmReloadES: ""Voulez-vous vraiment recharger la liste des jeux ? Cela redémarrera l'interface de EmulationStation."",
                esReloaded: ""Interface rechargée avec succès."",
                esReloadFailed: ""Échec du rechargement."",
                confirmReloadTitle: ""Confirmation de Rechargement"",
                btnConfirmReload: ""CONFIRMER"",
                btnCancelReload: ""ANNULER""
            },
            en: {
                loginTitle: ""Operator Access"",
                loginDesc: ""Please enter the machine password."",
                btnLogin: ""LOGIN"",
                mainTitle: ""BatRun Operator Dashboard"",
                statusRemaining: ""TIME REMAINING"",
                statusCoin: ""INSERT COIN"",
                statusFreeActive: ""FREE PLAY MODE ACTIVE"",
                credits: "" CREDITS"",
                labelAdd1: ""+1 CREDIT"",
                labelAdd10: ""+10 CREDITS"",
                labelRemove1: ""-1 CREDIT"",
                labelFreePlay: ""FREE PLAY"",
                labelForceLock: ""FORCE LOCK (GAME OVER)"",
                titleSession: ""Direct Session Settings"",
                descSession: ""Overrides current time without affecting credits."",
                btnSetTime: ""SET DURATION"",
                titleCreditParams: ""Credit Settings"",
                descCreditParams: ""Sets time added per credit."",
                btnSaveMPC: ""SAVE SETTING"",
                titleCloud: ""Mode / Cloud (External Access)"",
                descCloud: ""IP used by external clients. Leave empty to use STUN discovery (Google/Cloudflare)."",
                btnSavePublicIp: ""SAVE"",
                titleMessage: ""Send message to screen"",
                btnSendMessage: ""SEND MESSAGE"",
                msgPlaceholder: ""Ex: Maintenance in 5 min..."",
                timeMins: ""MINS"",
                btnLocalOperator: ""🛠️ OPERATOR MODE"",
                confirmLock: ""Do you really want to lock the session? This will trigger an immediate GAME OVER."",
                confirmFreePlay: ""Do you want to toggle FREE PLAY mode? This affects time countdown."",
                confirmOperator: ""Do you want to toggle the OPERATOR mode on this local machine?"",
                passWrong: ""Incorrect password"",
                apiError: ""Action failed"",
                networkTitle: ""BatRun Network"",
                networkDesc: ""Select a terminal to manage it"",
                gameSystem: ""SYSTEM"",
                operatorBadge: ""OPERATOR MODE"",
                gameTitleLabel: ""GAME"",
                gameExe: ""EXECUTABLE"",
                gameTime: ""DURATION"",
                historyBtn: ""📜 Game History"",
                historyTitle: ""Latest Play Sessions"",
                colSystem: ""SYSTEM"",
                colTitle: ""GAME"",
                colDuration: ""DURATION"",
                colDate: ""DATE"",
                labelSecurity: ""SECURITY"",
                securityTitle: ""Security & Access Logs"",
                noBlockedIps: ""No banned IPs"",
                noSecurityLogs: ""No logs available"",
                labelLogDate: ""History:"",
                optLiveLogs: ""🔴 LIVE (Active session)"",
                gameUnknown: ""RETROBAT / IDLE"",
                propTitle: ""Machine Properties"",
                propMachineLabel: ""Machine:"",
                propMacLabel: ""MAC Address:"",
                propIpHistoryLabel: ""IP History:"",
                propDeleteLabel: ""REMOVE FROM LIST"",
                noIpKnown: ""No known IP"",
                labelAddMachine: ""ADD MACHINE (IP)"",
                promptAddIp: ""Enter the IP address of the terminal to add :"",
                labelHideOpButtons: ""Hide floating buttons (LOCK/BR)"",
                labelUsers: ""USERS"",
                usersTitle: ""Users Management"",
                usersSub: ""Public access approvals"",
                thUsername: ""USERNAME"",
                thStatus: ""STATUS"",
                thActions: ""ACTIONS"",
                statusApproved: ""Approved"",
                statusRejected: ""Rejected"",
                statusPending: ""Pending"",
                labelLauncher: ""GAME LAUNCHER"",
                launcherTitle: ""Game Explorer"",
                esWarnTitle: ""EmulationStation API not found"",
                esWarnDesc: ""Search is disabled because ES Web Server is not responding. Enable it in RetroBat (Developer Options)."",
                gameSearchPlaceholder: ""Search for a game..."",
                mdAvailLabel: ""AVAILABILITY:"",
                btnLaunchNow: ""🚀 LAUNCH ON SELECTED"",
                availOk: ""Available"",
                availMissing: ""Missing"",
                playingBadge: ""PLAYING"",
                launchingBadge: ""LAUNCHING..."",
                btnStopGame: ""🛑 STOP GAME"",
                allSystems: ""ALL SYSTEMS"",
                sortSystem: ""Sort: System (A-Z)"",
                sortName: ""Sort: Name (A-Z)"",
                labelReloadES: ""RELOAD GAMES (ES)"",
                confirmReloadES: ""Do you really want to reload the games list? This will restart EmulationStation's interface."",
                esReloaded: ""Interface reloaded successfully."",
                esReloadFailed: ""Reload failed."",
                confirmReloadTitle: ""Reload Confirmation"",
                btnConfirmReload: ""CONFIRM"",
                btnCancelReload: ""CANCEL""
            }
        };

        let currentLang = localStorage.getItem('batrun_lang') || (navigator.language.startsWith('fr') ? 'fr' : 'en');
        let operatorPassword = sessionStorage.getItem('op_pass');
        let isArcadeEnabled = false;
        let isFreePlay = false;
        let isHomeView = true;
        let apiBaseUrl = """";

        function setLanguage(lang) {
            currentLang = lang;
            localStorage.setItem('batrun_lang', lang);
            const t = translations[lang];
            
            const ids = [
                'loginTitle', 'loginDesc', 'btnLogin', 'mainTitle', 'btnSetTime', 'btnSaveMPC',
                'titleSession', 'descSession', 'titleCreditParams', 'descCreditParams',
                'titleCloud', 'descCloud', 'btnSavePublicIp',
                'titleMessage', 'btnSendMessage', 'btnLocalOperator', 'networkTitle', 'networkDesc',
                'btnHistory', 'labelUsers', 'usersTitle', 'usersSub', 'thUsername', 'thStatus',
                'thActions', 'historyTitle', 'thSystem', 'thTitle', 'thDuration', 'thDate',
                'propTitle', 'propMachineLabel', 'propMacLabel', 'propIpHistoryLabel',
                'propDeleteLabel', 'labelAddMachine', 'labelHideOpButtons', 'labelLauncher',
                'launcherTitle', 'esWarnTitle', 'esWarnDesc', 'mdAvailLabel', 'btnLaunchNow',
                'labelAdd1', 'labelAdd10', 'labelRemove1', 'labelFreePlay', 'labelForceLock',
                'labelSecurity', 'securityTitle', 'labelLogDate', 'optLiveLogs'
            ];
            ids.forEach(id => {
                const el = document.getElementById(id);
                if (el && t[id]) el.innerText = t[id];
            });

            if (document.getElementById('creditsDisplay')) document.getElementById('creditsDisplay').innerText = (typeof data !== 'undefined' ? data.credits : '0') + t.credits;
            if (document.getElementById('operator-badge')) document.getElementById('operator-badge').innerText = t.operatorBadge;
            if (document.getElementById('opMessage')) document.getElementById('opMessage').placeholder = t.msgPlaceholder;

            const btnManual = document.getElementById('btnManual');
            if (btnManual) btnManual.innerText = lang === 'fr' ? '[M] MANUEL UTILISATEUR' : '[M] USER MANUAL';

            const systemFilter = document.getElementById('systemFilter');
            if (systemFilter && systemFilter.options[0]) systemFilter.options[0].text = t.allSystems;
            
            const sortFilter = document.getElementById('sortFilter');
            if (sortFilter && sortFilter.options.length > 1) {
                sortFilter.options[0].text = t.sortSystem;
                sortFilter.options[1].text = t.sortName;
            }

            const delSpan = document.getElementById('propDeleteLabel');
            if (delSpan) delSpan.innerText = t.propDeleteLabel;

            const reloadBtn = document.getElementById('btnReloadES');
            if (reloadBtn) reloadBtn.innerHTML = `🔄 ${t.labelReloadES}`;
            const reloadLauncherBtn = document.getElementById('btnReloadSelectedES');
            if (reloadLauncherBtn) reloadLauncherBtn.title = t.labelReloadES;

            ['confirmReloadTitle', 'btnConfirmReload', 'btnCancelReload'].forEach(id => {
                const el = document.getElementById(id);
                if (el) el.innerText = t[id];
            });

            if (document.getElementById('btnLoadMore')) document.getElementById('btnLoadMore').innerText = lang === 'fr' ? 'VOIR PLUS' : 'LOAD MORE';

            document.querySelectorAll('.flag').forEach(f => {
                if(f.id.includes('fr')) f.classList.toggle('active', lang === 'fr');
                if(f.id.includes('en')) f.classList.toggle('active', lang === 'en');
            });
            
            if (isHomeView) updateNetwork();
            else updateStatus();

            if (document.getElementById('securityModal') && document.getElementById('securityModal').style.display === 'block') loadSecurityLogs();
        }


        async function showLauncher() {
            isHomeView = false;
            document.getElementById('home-view').style.display = 'none';
            document.getElementById('mainContent').style.display = 'none';
            document.getElementById('launcher-view').style.display = 'block';
            
            // Check ES Status
            try {
                const res = await fetch('/api/es/status');
                const data = await res.json();
                document.getElementById('esStatusWarning').style.display = data.isOnline ? 'none' : 'block';
                document.getElementById('gameSearchInput').disabled = !data.isOnline;
                document.getElementById('btnSearch').disabled = !data.isOnline;
            } catch(e) {
                document.getElementById('esStatusWarning').style.display = 'block';
            }
            
            updateLauncherMachines();
            loadFilterSystems();
        }

        function updateLauncherMachines() {
            const list = document.getElementById('machineLaunchList');
            if (!list) return;
            list.innerHTML = '';
            
            // Get all unique discovered machines from _machines global
            const machines = Object.values(_machines).filter(m => m.isOnline);
            machines.forEach(m => {
                const item = document.createElement('label');
                item.className = 'machine-toggle';
                item.innerHTML = `
                    <input type=""checkbox"" name=""targetMachines"" autocomplete=""off"" value=""${m.isLocal ? 'local' : m.name}"" onchange=""this.parentElement.classList.toggle('active', this.checked); updateLauncherBanner();"">
                    <span class=""dot""></span>
                    <span>${m.name}</span>
                `;
                if (m.isLocal) {
                    item.classList.add('active');
                    item.querySelector('input').checked = true;
                }
                list.appendChild(item);
            });
        }

        async function loadFilterSystems() {
            const select = document.getElementById('systemFilter');
            if (!select) return;
            if (select.children.length > 1) return; // Prevent double load

            try {
                const res = await fetch('/api/es/systems');
                if (!res.ok) throw new Error('Systems API error');
                const systems = await res.json();
                console.log('Loaded systems:', systems);
                
                systems.sort((a,b) => (a.fullname || a.name).localeCompare(b.fullname || b.name)).forEach(s => {
                    const opt = document.createElement('option');
                    opt.value = s.name;
                    opt.innerText = s.fullname || s.name.toUpperCase();
                    select.appendChild(opt);
                });
            } catch(e) {
                console.error('Failed to load systems:', e);
            }
        }

        window.onscroll = function() {
            const btn = document.getElementById('backToTop');
            if (!btn) return;
            if (document.body.scrollTop > 300 || document.documentElement.scrollTop > 300) {
                btn.classList.add('show');
            } else {
                btn.classList.remove('show');
            }
        };

        function scrollToTop() {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        }

        let searchTimer = null;
        function searchGamesDebounced() {
            if (searchTimer) clearTimeout(searchTimer);
            searchTimer = setTimeout(() => {
                searchGames();
            }, 300);
        }

        let currentGamesList = [];
        let renderPage = 0;
        const RENDER_CHUNK = 50;

        let pendingReloadMachines = [];

        function closeReloadConfirm() {
            document.getElementById('reloadConfirmModal').style.display = 'none';
            pendingReloadMachines = [];
        }

        async function openReloadConfirm(machineValues) {
            const t = translations[currentLang];
            const listUl = document.getElementById('reloadMachinesList');
            listUl.innerHTML = '';
            pendingReloadMachines = machineValues;

            machineValues.forEach(mVal => {
                let name = 'Unknown';
                let ip = '';
                
                // Trouve l'objet machine correspondant dans _machines
                const machineObj = Object.values(_machines).find(m => {
                    const checkVal = m.isLocal ? 'local' : m.name;
                    return checkVal === mVal;
                });

                if (machineObj) {
                    name = machineObj.name || 'Unknown';
                    ip = machineObj.isLocal ? '127.0.0.1 (Local)' : machineObj.ip;
                } else if (mVal === 'local') {
                    name = 'Local Machine';
                    ip = '127.0.0.1';
                }

                const li = document.createElement('li');
                li.style.padding = '5px 0';
                li.style.borderBottom = '1px solid rgba(255,255,255,0.05)';
                li.innerHTML = `<span style=""color: var(--primary);"">●</span> <strong>${name}</strong> <span style=""opacity:0.6; font-size:0.8rem;"">(${ip})</span>`;
                listUl.appendChild(li);
            });

            document.getElementById('reloadConfirmModal').style.display = 'block';
        }

        async function confirmReloadES() {
            const machines = [...pendingReloadMachines];
            closeReloadConfirm();
            
            for (const m of machines) {
                await executeReloadAction(m);
            }
        }

        async function executeReloadAction(mVal) {
            const t = translations[currentLang];
            try {
                // Check if game is running on that machine if possible (local check)
                if (mVal === 'local' && (typeof _isGameLaunching !== 'undefined' || typeof currentGameName !== 'undefined')) {
                    const isBusy = (typeof currentGameName !== 'undefined' && currentGameName !== '' && currentGameName !== t.gameUnknown);
                    if (isBusy) {
                        alert(currentLang === 'fr' ? 'Action impossible : Un jeu est en cours.' : 'Action impossible: Game in progress.');
                        return;
                    }
                }

                const url = mVal === 'local' ? '/api/action' : `/api/relay?target=${mVal}&path=/api/action`;
                const r = await fetch(url, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ action: 'reload_es', password: operatorPassword })
                });

                if (r.ok) {
                    // Success, maybe a small notification logic here later
                } else if (r.status === 409) {
                    alert(`${currentLang === 'fr' ? 'Jeu en cours sur' : 'Game in progress on'} ${mVal}`);
                }
            } catch(e) { console.error('Reload failed for', mVal, e); }
        }

        async function reloadEs(targetMachine = null) {
            // [BATRUN-HUB-FIX]: Use machine name as relay target instead of ip:port (ip is 'REMOTE' in HubMode)
            // FR: Utiliser le nom de machine comme cible relay au lieu de ip:port (ip vaut 'REMOTE' en HubMode)
            const mVal = targetMachine || (currentMachineProp ? (currentMachineProp.isLocal ? 'local' : currentMachineProp.name) : null);
            if (!mVal) return;
            openReloadConfirm([mVal]);
        }

        async function reloadSelectedEs() {
            const selectedMachines = Array.from(document.querySelectorAll('input[name=""targetMachines""]:checked')).map(i => i.value);
            if (selectedMachines.length === 0) {
                alert(currentLang === 'fr' ? 'Sélectionnez au moins une borne !' : 'Select at least one machine!');
                return;
            }
            openReloadConfirm(selectedMachines);
        }

        async function searchGames() {
            const query = document.getElementById('gameSearchInput').value;
            const system = document.getElementById('systemFilter').value;
            const container = document.getElementById('searchResults');
            
            const cacheKey = query + '|' + system;
            if (container.dataset.lastQuery === cacheKey && query !== '') return;
            container.dataset.lastQuery = cacheKey;

            container.innerHTML = '<div style=""grid-column: 1/-1; text-align:center; padding:20px; opacity:0.5;"">⌛ ...</div>';
            
            try {
                let selectedMachines = Array.from(document.querySelectorAll('input[name=""targetMachines""]:checked')).map(i => i.value);
                if (selectedMachines.length === 0) selectedMachines = ['local'];

                const fetchPromises = selectedMachines.map(async (mVal) => {
                    const params = `q=${encodeURIComponent(query)}&system=${encodeURIComponent(system)}`;
                    const url = mVal === 'local' ? `/api/es/games?${params}` : `/api/relay?target=${mVal}&path=/api/es/games?${params}`;
                    try {
                        const res = await fetch(url);
                        if (!res.ok) return [];
                        const games = await res.json();
                        games.forEach(g => g._originMachine = mVal);
                        return games;
                    } catch(e) { return []; }
                });

                const resultsArray = await Promise.all(fetchPromises);
                
                const mergedGames = [];
                const seenKeys = new Set();
                
                resultsArray.flat().forEach(g => {
                    const key = (g.system || 'unknown') + '|' + (g.path || g.name || '');
                    if (!seenKeys.has(key)) {
                        seenKeys.add(key);
                        mergedGames.push(g);
                    }
                });

                // Triage dynamique
                const sortType = document.getElementById('sortFilter') ? document.getElementById('sortFilter').value : 'system';
                mergedGames.sort((a, b) => {
                    if (sortType === 'system') {
                        const sysCmp = (a.system || '').localeCompare(b.system || '');
                        if (sysCmp !== 0) return sysCmp;
                        return (a.name || '').localeCompare(b.name || '');
                    } else {
                        return (a.name || '').localeCompare(b.name || '');
                    }
                });

                currentGamesList = mergedGames;
                renderPage = 0;
                container.innerHTML = '';
                
                if (currentGamesList.length === 0) {
                    container.innerHTML = '<div style=""grid-column: 1/-1; text-align:center; padding:20px; opacity:0.5;"">Aucun résultat</div>';
                    return;
                }

                renderGamesChunk();
            } catch(e) {
                container.innerHTML = '<div style=""grid-column: 1/-1; text-align:center; padding:20px; color:var(--danger)"">Erreur de recherche</div>';
            }
        }

        let scrollObserver = null;

        function renderGamesChunk() {
            const container = document.getElementById('searchResults');
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
                const card = document.createElement('div');
                card.className = 'game-item';
                card.dataset.gameName = g.name;
                card.dataset.gameSystem = g.system;
                card.onclick = () => showGameDetails(g);
                
                const fallbackImg = g.thumbnail || g.image || g.fanart || g.marquee;
                let imgSrc = '';
                if (fallbackImg) {
                    if (g._originMachine === 'local') {
                        imgSrc = '/api/es/media?path=' + encodeURIComponent(fallbackImg);
                    } else {
                        imgSrc = `/api/relay?target=${g._originMachine}&path=/api/es/media?path=${encodeURIComponent(fallbackImg)}`;
                    }
                }
                
                const isPlaying = Object.values(_machines).some(m => {
                    if (!m.isOnline) return false;
                    if (m.currentGameSystem?.toLowerCase() !== g.system?.toLowerCase()) return false;
                    const mName = normalizeGameName(m.currentGameName);
                    const gRaw = normalizeGameName(g.name);
                    return mName !== '' && mName === gRaw;
                });

                // EN: Check if any selected machine is occupied (for button disable)
                // FR: Vérifie si une borne sélectionnée est déjà occupée (pour désactiver le bouton)
                const anyBusy = isAnySelectedMachinePlaying();

                card.innerHTML = `
                    <div class=""playing-badge"" style=""display: ${isPlaying ? 'flex' : 'none'}"">${translations[currentLang].playingBadge}</div>
                    ${imgSrc ? `<img src=""${imgSrc}"" loading=""lazy"" onerror=""this.style.display='none'; this.nextElementSibling.style.display='flex';"">` : ''}
                    <div style=""aspect-ratio:3/4; background:rgba(0,0,0,0.3); border-radius:8px; display:${imgSrc ? 'none' : 'flex'}; align-items:center; justify-content:center; margin-bottom:8px;"">🎮</div>
                    <div class=""game-item-title"">${g.name}</div>
                    <div class=""game-item-system"">${g.system}</div>
                `;

                const playBtn = document.createElement('div');
                playBtn.className = 'game-play-btn' + (anyBusy ? ' disabled-btn' : '');
                playBtn.innerText = anyBusy ? '🔒' : '▶';
                playBtn.title = anyBusy ? (currentLang === 'fr' ? 'Un jeu est déjà en cours' : 'A game is already running') : '';
                playBtn.onclick = (e) => { e.stopPropagation(); quickLaunchGame(g, card); };
                card.appendChild(playBtn);
                
                fragment.appendChild(card);
            }
            
            container.appendChild(fragment);
            
            if (end < currentGamesList.length) {
                const trigger = document.createElement('div');
                trigger.id = 'infiniteScrollTrigger';
                trigger.style = ""grid-column: 1/-1; height: 50px; display: flex; align-items: center; justify-content: center; opacity: 0.5; font-size: 0.8rem;"";
                trigger.innerHTML = ""<span>⌛ Chargement...</span>"";
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

        let currentSelectedGame = null;

        async function showGameDetails(game) {
            currentSelectedGame = game;
            const t = translations[currentLang];
            document.getElementById('mdTitle').innerText = game.name;
            document.getElementById('mdSystem').innerText = game.system;
            
            const img = document.getElementById('mdImage');
            const video = document.getElementById('mdVideo');
            
            const isLocal = !game._originMachine || game._originMachine === 'local';
            const relayPrefix = isLocal ? '' : `/api/relay?target=${game._originMachine}&path=`;

            if (game.image) {
                const imgUrl = '/api/es/media?path=' + encodeURIComponent(game.image);
                img.src = isLocal ? imgUrl : `${relayPrefix}${encodeURIComponent(imgUrl)}`;
                img.style.display = 'block';
            } else {
                img.style.display = 'none';
            }

            // EN: Pre-fill with available data from search results (essential for collections)
            // FR: Remplissage initial avec les données de recherche (essentiel pour les collections)
            video.style.display = 'none';
            video.src = '';
            if (game.video) {
                const videoUrl = '/api/es/media?path=' + encodeURIComponent(game.video);
                video.src = isLocal ? videoUrl : `${relayPrefix}${encodeURIComponent(videoUrl)}`;
                video.style.display = 'block';
                img.style.display = 'none'; // Hide image if video is playing
            }

            document.getElementById('mdDesc').innerText = game.Description || game.description || '...';
            document.getElementById('availList').innerHTML = '';
            document.getElementById('btnManual').style.display = (game.manual) ? 'block' : 'none';
            
            try {
                const metaUrl = '/api/es/metadata?system=' + encodeURIComponent(game.system) + '&path=' + encodeURIComponent(game.path);
                const fetchUrl = isLocal ? metaUrl : `${relayPrefix}${encodeURIComponent(metaUrl)}`;
                
                const metaRes = await fetch(fetchUrl);
                const metadata = await metaRes.json();
                
                // EN: Enrich from metadata, but don't clear existing search-result description
                // FR: Enrichir avec les métadonnées sans effacer la description existante
                if (metadata.desc) document.getElementById('mdDesc').innerText = metadata.desc;
                else if (!game.Description && !game.description) document.getElementById('mdDesc').innerText = 'Aucune description disponible.';

                if (metadata.video) {
                    const videoUrl = '/api/es/media?path=' + encodeURIComponent(metadata.video);
                    video.src = isLocal ? videoUrl : `${relayPrefix}${encodeURIComponent(videoUrl)}`;
                    video.style.display = 'block';
                    img.style.display = 'none'; // Hide image if video is found in metadata
                }

                if (metadata.manual || game.manual) {
                    const mPath = metadata.manual || game.manual;
                    const manUrl = '/api/es/media?path=' + encodeURIComponent(mPath);
                    document.getElementById('btnManual').style.display = 'block';
                    document.getElementById('btnManual').onclick = () => window.open(isLocal ? manUrl : `${relayPrefix}${encodeURIComponent(manUrl)}`, '_blank');
                }
            } catch(e) {
                if (!game.Description && !game.description)
                    document.getElementById('mdDesc').innerText = 'Erreur lors du chargement des métadonnées.';
            }

            document.getElementById('gameDetailsModal').style.display = 'block';

            const selectedMachines = Array.from(document.querySelectorAll('input[name=""targetMachines""]:checked')).map(i => i.value);
            const list = document.getElementById('availList');
            
            selectedMachines.forEach(mVal => {
                let mName = ""?"";
                Object.values(_machines).forEach(machine => {
                    // [BATRUN-HUB-FIX]: Match by name (checkbox values use m.name, not ip:port)
                    // FR: Matcher par nom (les checkbox utilisent m.name, pas ip:port)
                    const machineVal = machine.isLocal ? 'local' : machine.name;
                    if (machineVal === mVal) mName = machine.name;
                });

                const row = document.createElement('div');
                row.style = 'display:flex; align-items:center; justify-content:space-between; font-size:0.85rem; padding:4px 8px; background:rgba(255,255,255,0.03); border-radius:6px;';
                row.innerHTML = `<span>${mName}</span> <span class=""avail-status""><span class=""availability-dot""></span> ...</span>`;
                list.appendChild(row);

                checkAvailability(game, mVal, row.querySelector('.avail-status'));
            });
        }

        async function checkAvailability(game, machine, element) {
            const t = translations[currentLang];
            try {
                const url = machine === 'local' ? `/api/es/games?q=${encodeURIComponent(game.name)}` : `/api/relay?target=${machine}&path=/api/es/games?q=${encodeURIComponent(game.name)}`;
                const res = await fetch(url);
                const results = await res.json();
                const found = results.some(r => r.path === game.path || r.name === game.name);
                
                if (found) {
                    element.innerHTML = `<span class=""availability-dot available""></span> ${t.availOk}`;
                } else {
                    element.innerHTML = `<span class=""availability-dot""></span> <span style=""opacity:0.5"">${t.availMissing}</span>`;
                }
            } catch(e) {
                element.innerHTML = `<span style=""color:var(--danger)"">OFFLINE</span>`;
            }
        }

        function closeGameDetails() {
            document.getElementById('mdVideo').pause();
            document.getElementById('gameDetailsModal').style.display = 'none';
        }

        function setGameLaunchingState(game) {
            if (!game) return;
            const cards = document.querySelectorAll('.game-item');
            cards.forEach(card => {
                const gName = card.dataset.gameName;
                const gSystem = card.dataset.gameSystem;
                if (gName === game.name && gSystem === game.system) {
                    const badge = card.querySelector('.playing-badge');
                    if (badge) {
                        badge.innerHTML = translations[currentLang].launchingBadge;
                        badge.style.background = 'linear-gradient(135deg, #00b09b, #96c93d)';
                        badge.style.display = 'flex';
                        badge.dataset.launching = 'true';
                        setTimeout(() => { badge.dataset.launching = 'false'; }, 5000);
                    }
                }
            });
        }

        function isAnySelectedMachinePlaying() {
            let selectedMachines = Array.from(document.querySelectorAll('input[name=""targetMachines""]:checked')).map(i => i.value);
            if (selectedMachines.length === 0) selectedMachines = ['local'];
            
            for (let j = 0; j < selectedMachines.length; j++) {
                const targetVal = selectedMachines[j];
                const machines = Object.values(_machines);
                for (let k = 0; k < machines.length; k++) {
                    const m = machines[k];
                    const matchVal = m.isLocal ? 'local' : m.name;
                    if (matchVal === targetVal && m.currentGameName && m.currentGameName !== 'Idle / Menu') {
                        return true;
                    }
                }
            }
            return false;
        }

        async function quickLaunchGame(game, element) {
            if (isAnySelectedMachinePlaying()) {
                alert(currentLang === 'fr' ? 'Un jeu est déjà en cours ! Arrêtez-le avant d\'en lancer un nouveau.' : 'A game is already running! Stop it before launching a new one.');
                return;
            }
            currentSelectedGame = game;
            
            if (element) {
                element.classList.add('crush-impact');
            }
            setGameLaunchingState(game);

            let selectedMachines = Array.from(document.querySelectorAll('input[name=""targetMachines""]:checked')).map(i => i.value);
            if (selectedMachines.length === 0) selectedMachines = ['local'];

            console.log('QuickLaunch targeting:', selectedMachines);

            for(let j = 0; j < selectedMachines.length; j++) {
                const mVal = selectedMachines[j];
                try {
                    const url = mVal === 'local' ? '/api/action' : `/api/relay?target=${mVal}&path=/api/action`;
                    const payload = { action: 'launch_game', gamePath: currentSelectedGame.path, password: operatorPassword };
                    
                    const r = await fetch(url, { 
                        method: 'POST', 
                        headers: { 'Content-Type': 'application/json' }, 
                        body: JSON.stringify(payload) 
                    });
                    
                    if (!r.ok && r.status === 401) {
                        const np = prompt(currentLang === 'fr' ? 'Mot de passe op\u00e9rateur requis :' : 'Operator password required:');
                        if (np) { operatorPassword = np; sessionStorage.setItem('op_pass', np); j--; }
                    }
                } catch(e) { console.error(""Launch error for"", mVal, e); }
            }
            
            // Wait for animation to finish before potential refresh or state change
            if (element) {
                setTimeout(() => { element.classList.remove('crush-impact'); }, 600);
            }
        }

        async function launchSelectedGame() {
            if (isAnySelectedMachinePlaying()) {
                alert(currentLang === 'fr' ? 'Un jeu est déjà en cours ! Arrêtez-le avant d\'en lancer un nouveau.' : 'A game is already running! Stop it before launching a new one.');
                return;
            }
            if (!currentSelectedGame) return;
            const modalContent = document.querySelector('#gameDetailsModal .modal-content');
            
            if (modalContent) {
                modalContent.classList.add('crush-impact');
            }
            setGameLaunchingState(currentSelectedGame);

            let selectedMachines = Array.from(document.querySelectorAll('input[name=""targetMachines""]:checked')).map(i => i.value);

            if (selectedMachines.length === 0) {
                alert(currentLang === 'fr' ? 'S\u00e9lectionnez au moins une borne !' : 'Select at least one machine!');
                if (modalContent) modalContent.classList.remove('crush-impact');
                return;
            }

            for(let j = 0; j < selectedMachines.length; j++) {
                const mVal = selectedMachines[j];
                try {
                    const url = mVal === 'local' ? '/api/action' : `/api/relay?target=${mVal}&path=/api/action`;
                    const payload = { action: 'launch_game', gamePath: currentSelectedGame.path, password: operatorPassword };
                    
                    const r = await fetch(url, { 
                        method: 'POST', 
                        headers: { 'Content-Type': 'application/json' }, 
                        body: JSON.stringify(payload) 
                    });
                    
                    if (!r.ok && r.status === 401) {
                        const np = prompt(currentLang === 'fr' ? 'Mot de passe op\u00e9rateur requis :' : 'Operator password required:');
                        if (np) { operatorPassword = np; sessionStorage.setItem('op_pass', np); j--; }
                    }
                } catch(e) { console.error(""Launch failed for "" + mVal, e); }
            }

            // EN: Wait for animation effect before closing / FR: Attendre l'effet d'animation avant de fermer
            setTimeout(() => {
                if (modalContent) modalContent.classList.remove('crush-impact');
                closeGameDetails();
            }, 600);
        }

        function showHome() {
            isHomeView = true;
            apiBaseUrl = """"; // Reset to local when going home
            document.getElementById('home-view').style.display = 'block';
            document.getElementById('mainContent').style.display = 'none';
            document.getElementById('launcher-view').style.display = 'none';
            document.getElementById('users-view').style.display = 'none';
            document.getElementById('loginOverlay').style.display = 'none';
            updateNetwork();
        }

        function showDashboard(machineName = null) {
            isHomeView = false;
            if (machineName) {
                // [BATRUN-HUB]: Use machine name as alias instead of raw IP:port
                apiBaseUrl = `/api/relay?target=${encodeURIComponent(machineName)}&path=`;
            } else {
                apiBaseUrl = """";
            }
            document.getElementById('home-view').style.display = 'none';
            document.getElementById('users-view').style.display = 'none';
            document.getElementById('mainContent').style.display = 'block';
            updateStatus();
        }

        // EN: Check for view parameter on startup to show dashboard directly
        // FR: Vérifier le paramètre view au démarrage pour afficher le dashboard directement
        window.addEventListener('load', () => {
            const urlParams = new URLSearchParams(window.location.search);
            if (urlParams.get('view') === 'dashboard') {
                showDashboard();
            }
        });

        // EN: Registry of latest machine objects by name (for the properties button)
        // FR: Registre des dernières machines par nom (pour le bouton propriétés)
        const _machines = {};

        // EN: Current machine selected for properties modal
        // FR: Borne sélectionnée pour la modale propriétés
        let currentMachineProp = null;

        function addManualMachine() {
            const ip = prompt(translations[currentLang].promptAddIp);
            if (!ip || ip.trim() === """") return;
            fetch('/api/action', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ action: 'add_machine', ip: ip.trim(), password: operatorPassword })
            }).then(() => updateNetwork());
        }

        function showMachineProperties(m) {
            currentMachineProp = m;
            document.getElementById('propName').textContent = m.name || 'Unknown';
            document.getElementById('propMac').textContent = m.macAddress && m.macAddress !== '' ? m.macAddress : 'UNKNOWN_MAC';
            const ul = document.getElementById('propIpHistory');
            ul.innerHTML = '';
            
            // EN: Handle IpHistory which is a HashSet (serialized as array)
            // FR: Gérer IpHistory qui est un HashSet (sérialisé comme tableau)
            let ips = [];
            if (m.ipHistory && Array.isArray(m.ipHistory)) {
                ips = m.ipHistory;
            } else if (m.ip) {
                ips = [m.ip];
            }
            
            // EN: Filter duplicates and empty strings
            // FR: Filtrer les doublons et les chaînes vides
            const uniqueIps = [...new Set(ips)].filter(ip => ip && ip.trim() !== '');
            
            if (uniqueIps.length === 0) {
                const li = document.createElement('li');
                li.textContent = translations[currentLang].noIpKnown || 'Aucune IP connue';
                ul.appendChild(li);
            } else {
                uniqueIps.forEach(ip => { 
                    const li = document.createElement('li'); 
                    li.textContent = '- ' + ip; 
                    ul.appendChild(li); 
                });
            }
            document.getElementById('propModal').style.display = 'block';
        }

        function closeProperties() {
            document.getElementById('propModal').style.display = 'none';
        }

        function removeMachine() {
            if (!currentMachineProp) return;
            const t = translations[currentLang];
            const label = currentLang === 'fr' ? 'Mot de passe opérateur requis pour supprimer :' : 'Operator password required to delete:';
            const pwd = prompt(label);
            if (pwd !== null) {
                fetch('/api/action', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ 
                        action: 'remove_machine', 
                        machineName: currentMachineProp.name, 
                        macAddress: currentMachineProp.macAddress,
                        password: pwd 
                    })
                }).then(r => {
                    if (r.ok) {
                        closeProperties();
                        updateNetwork();
                    } else {
                        alert(t.passWrong || 'Mot de passe incorrect.');
                    }
                });
            }
        }

        async function updateNetwork() {
            try {
                const response = await fetch('/api/network');
                if (!response.ok) throw new Error('Network response was not ok');
                const machines = await response.json();
                
                // EN: Always update the registry / FR: Toujours mettre à jour le registre
                machines.forEach(m => {
                    const mName = m.name || 'Unknown';
                    _machines[mName] = m;
                });

                if (!isHomeView) return;

                console.log('Detected machines:', machines);
                const grid = document.getElementById('network-grid');
                if (!grid) return;
                
                grid.innerHTML = '';
                document.getElementById('network-count').innerText = machines.length + ' machine(s) ' + (currentLang === 'fr' ? 'détectée(s)' : 'detected');
                
                machines.sort((a,b) => (b.isOnline - a.isOnline) || a.name.localeCompare(b.name));

                // EN: Show all machines including local
                // FR: Afficher toutes les machines y compris la locale
                const visibleMachines = machines;

                // EN: Remove cards for machines no longer in the list
                // FR: Retirer les cartes des machines qui ne sont plus dans la liste
                const currentNames = new Set(visibleMachines.map(m => m.name));
                Array.from(grid.children).forEach(c => {
                    if (!currentNames.has(c.dataset.machineName)) grid.removeChild(c);
                });

                visibleMachines.forEach(m => {
                    // EN: Keep machine registry updated (latest live object for properties button)
                    // FR: Mise à jour du registre (dernier objet vivant pour le bouton propriétés)
                    const mName = m.name || 'Unknown';
                    _machines[mName] = m;

                    const cardId = 'card-' + mName.replace(/[^a-zA-Z0-9]/g, '-');
                    let card = document.getElementById(cardId);
                    const isNew = !card;

                    if (isNew) {
                        card = document.createElement('div');
                        card.className = 'machine-card';
                        card.onclick = () => {
                            if (m.isLocal) showDashboard();
                            else showDashboard(m.name); // [BATRUN-HUB]: Use machine name as alias for relay
                        };
                        card.innerHTML = `
                            ${m.isLocal ? '<div class=""local-badge"">LOCAL</div>' : ''}
                            <button class=""machine-props-btn"" onclick=""event.stopPropagation(); showMachineProperties(_machines['${mName}'])"">&#8943;</button>
                            <div class=""machine-name-container"">
                                <span class=""machine-name""></span>
                            </div>
                            <div class=""machine-game-container"" style=""display:none"">
                                <span class=""machine-game-system""></span>
                                <span class=""machine-game-title""></span>
                            </div>
                            <div class=""badges-row"" style=""display:flex;gap:5px;justify-content:center;""></div>
                            <div class=""game-duration"" style=""font-size:1.2rem;""></div>
                        `;
                        card.dataset.machineName = mName;
                        grid.appendChild(card);
                    }

                    // EN: Update state without rebuilding - preserves animations
                    // FR: Mettre à jour l'état sans reconstruire - préserve les animations
                    card.className = 'machine-card' + (m.isOnline ? '' : ' offline');
                    card.querySelector('.machine-name').textContent = mName;

                    const gameContainer = card.querySelector('.machine-game-container');
                    const hasGame = m.currentGameName && m.currentGameName !== 'Idle / Menu' && m.currentGameName !== '';
                    if (hasGame) {
                        card.querySelector('.machine-game-system').textContent = m.currentGameSystem || '';
                        card.querySelector('.machine-game-title').textContent = m.currentGameName || '';
                        gameContainer.style.display = '';
                    } else {
                        gameContainer.style.display = 'none';
                    }

                    const badgeRow = card.querySelector('.badges-row');
                    badgeRow.innerHTML = [
                        m.isFreePlay ? '<span class=""status-badge badge-freeplay"" style=""visibility:visible;font-size:0.55rem;padding:2px 6px"">FREE</span>' : '',
                        m.isOperatorUnlocked ? '<span class=""status-badge badge-operator"" style=""visibility:visible;font-size:0.55rem;padding:2px 6px"">OP</span>' : ''
                    ].join('');

                    const dot = m.isOnline ? 'dot-online' : 'dot-offline';
                    card.querySelector('.game-duration').innerHTML = `<span class=""status-dot ${dot}""></span> ${m.statusDisplay || m.timeRemaining || '--:--'}`;

                    if (isNew) {
                        // Animation or future logic
                    }
                });

                if (typeof updateLauncherBanner === 'function') updateLauncherBanner();
            } catch (e) { 
                console.error('UpdateNetwork failed:', e);
            }
        }

        function normalizeGameName(name) {
            if (!name) return '';
            try {
                let n = name.toLowerCase();
                n = n.replace(/\([^)]*\)/g, ''); // remove (text)
                n = n.replace(/\[[^\]]*\]/g, ''); // remove [text]
                if (n.lastIndexOf('.') > 0) n = n.substring(0, n.lastIndexOf('.'));
                return n.trim();
            } catch(e) { return name.toLowerCase().trim(); }
        }

        // EN: Update ""Now Playing"" badges on all rendered game cards
        // FR: Mettre à jour les badges ""EN COURS"" sur toutes les cartes affichées
        function refreshPlayingBadges() {
            const cards = document.querySelectorAll('.game-item');
            const machines = Object.values(_machines);

            cards.forEach(card => {
                const gName = card.dataset.gameName;
                const gSystem = card.dataset.gameSystem;
                if (!gName) return;

                const isPlaying = machines.some(m => {
                    if (!m.isOnline) return false;
                    if (m.currentGameSystem?.toLowerCase() !== gSystem?.toLowerCase()) return false;
                    const mName = normalizeGameName(m.currentGameName);
                    const gRaw = normalizeGameName(gName);
                    return mName !== '' && mName === gRaw;
                });

                const badge = card.querySelector('.playing-badge');
                if (badge) {
                    if (badge.dataset.launching === 'true') return; // Hide updates while launching
                    badge.innerHTML = translations[currentLang].playingBadge;
                    badge.style.background = 'linear-gradient(135deg, #ff416c, #ff4b2b)';
                    badge.style.display = isPlaying ? 'flex' : 'none';
                }
            });
            refreshLaunchButtonsState();
        }

        // EN: Visually lock/unlock all launch buttons based on machine state
        // FR: Verrouille/déverrouille visuellement tous les boutons de lancement
        function refreshLaunchButtonsState() {
            const busy = isAnySelectedMachinePlaying();
            const t = translations[currentLang];
            const lockLabel = currentLang === 'fr' ? 'Jeu en cours' : 'Game running';

            // EN: Update play buttons in game grid cards
            // FR: Mise à jour des boutons dans les cartes de jeu
            const playBtns = document.querySelectorAll('.game-play-btn');
            playBtns.forEach(btn => {
                if (btn.dataset.launching === 'true') return;
                if (busy) {
                    btn.classList.add('disabled-btn');
                    btn.innerText = '\uD83D\uDD12'; // lock emoji
                    btn.title = lockLabel;
                } else {
                    btn.classList.remove('disabled-btn');
                    btn.innerText = '\u25B6'; // play arrow
                    btn.title = '';
                }
            });

            // EN: Lock/unlock the launch button in the game details modal
            // FR: Verrouille/déverrouille le bouton LANCER dans la modale
            const modalBtn = document.getElementById('btnLaunchNow');
            if (modalBtn) {
                modalBtn.disabled = busy;
                modalBtn.style.opacity = busy ? '0.4' : '';
                modalBtn.style.cursor = busy ? 'not-allowed' : '';
                if (busy) {
                    modalBtn.innerText = '\uD83D\uDD12 ' + (currentLang === 'fr' ? 'LANCEMENT BLOQUÉ' : 'LAUNCH BLOCKED');
                } else {
                    modalBtn.innerText = '\uD83D\uDE80 ' + (currentLang === 'fr' ? 'LANCER SUR LES BORNES' : 'LAUNCH ON MACHINES');
                }
            }
        }

        async function updateStatus() {
            try {
            	const url = apiBaseUrl ? `${apiBaseUrl}/api/status` : '/api/status';
                const response = await fetch(url);
                const data = await response.json();
                const t = translations[currentLang];
                
                isFreePlay = data.isFreePlay;

                // Update local machine instantaneously
                Object.values(_machines).forEach(m => {
                    if (m.isLocal) {
                        m.currentGameName = data.currentGameName;
                        m.currentGameSystem = data.currentGameSystem;
                    }
                });
                if (typeof updateLauncherBanner === 'function') updateLauncherBanner();
                if (typeof refreshPlayingBadges === 'function') refreshPlayingBadges();
                if (typeof refreshLaunchButtonsState === 'function') refreshLaunchButtonsState();

                document.getElementById('creditsDisplay').innerText = data.credits + t.credits;
                document.getElementById('machineName').innerText = 'MACHINE: ' + data.machineName;
                
                document.getElementById('freeplay-badge').style.visibility = data.isFreePlay ? 'visible' : 'hidden';
                document.getElementById('operator-badge').style.visibility = data.isOperatorUnlocked ? 'visible' : 'hidden';

                const opBtn = document.getElementById('btnLocalOperator');
                if (opBtn) {
                    if (data.isOperatorUnlocked) {
                        opBtn.style.background = 'linear-gradient(135deg, #f39c12, #d35400)';
                        opBtn.innerText = '🔓 ' + (currentLang === 'fr' ? 'Désactiver Mode Opérateur' : 'Deactivate Operator Mode');
                    } else {
                        opBtn.style.background = '';
                        opBtn.innerText = t.btnLocalOperator;
                    }
                }

                const mpcInput = document.getElementById('mpcInput');
                if (mpcInput && document.activeElement !== mpcInput) {
                    mpcInput.value = data.minutesPerCredit;
                }

                const chkHide = document.getElementById('chkHideOpButtons');
                if (chkHide) {
                    chkHide.checked = data.hideOperatorButtons;
                }

                const pubIpInput = document.getElementById('publicIpInput');
                if (pubIpInput && document.activeElement !== pubIpInput) {
                    pubIpInput.value = data.publicIp || '';
                }
               
                if (data.requiresAuth && !operatorPassword) {
                    document.getElementById('loginOverlay').style.display = 'flex';
                }

                const statusLabel = document.getElementById('statusLabel');
                const timeDisplay = document.getElementById('timeDisplay');
                
                if (data.isFreePlay) {
                    statusLabel.innerText = t.statusFreeActive;
                    timeDisplay.innerText = 'FREE';
                    timeDisplay.classList.add('freeplay-mode');
                } else {
                    statusLabel.innerText = data.isLocked ? t.statusCoin : t.statusRemaining;
                    timeDisplay.innerText = data.timeRemaining;
                    timeDisplay.classList.remove('freeplay-mode');
                }

                // Update Game Info
                const gameInfo = document.getElementById('gameInfoContainer');
                if (data.currentGameSystem && data.currentGameName && data.currentGameName !== ""Idle / Menu"") {
                    gameInfo.style.display = 'flex';
                    document.getElementById('gameSystem').innerText = data.currentGameSystem.toUpperCase();
                    document.getElementById('gameTitle').innerText = data.currentGameName || ""---"";
                    document.getElementById('gameExe').innerText = data.currentExecutable ? ""exe: "" + data.currentExecutable.toLowerCase() + "".exe"" : """";
                    document.getElementById('gameDurationText').innerText = data.currentGameDuration || ""00:00"";
                } else {
                    gameInfo.style.display = 'none';
                }

                document.getElementById('btnFreePlay').style.borderColor = data.isFreePlay ? '#00b09b' : 'rgba(255,255,255,0.1)';



            } catch (e) {
                console.error('Update failed', e);
            }
        }



        function toggleHideOpButtons() {
            performAction('toggle_hide_operator_buttons');
        }

        function login() {
            const pass = document.getElementById('passInput').value;
            if (pass) {
                operatorPassword = pass;
                sessionStorage.setItem('op_pass', pass);

                // [BATRUN-CRED] EN: Ask browser to remember operator password via Credential Management API
                // FR: Demander au navigateur de mémoriser le mot de passe opérateur
                if (window.PasswordCredential) {
                    try {
                        const cred = new PasswordCredential({
                            id: 'operator',
                            password: pass,
                            name: 'BatRun Opérateur'
                        });
                        navigator.credentials.store(cred);
                    } catch(ce) { console.warn('[BatRun] credentials.store (admin) failed:', ce); }
                }

                document.getElementById('loginOverlay').style.display = 'none';
                updateStatus();
            }
        }

        let isHistoryOpen = false;
        function toggleHistory() {
            const modal = document.getElementById('historyModal');
            isHistoryOpen = !isHistoryOpen;
            modal.style.display = isHistoryOpen ? 'block' : 'none';
            if (isHistoryOpen) fetchHistory();
        }

        let fullHistory = [];
        let itemsShown = 0;
        const ITEMS_PER_PAGE = 15;

        async function fetchHistory() {
            const body = document.getElementById('historyBody');
            const pagin = document.getElementById('historyPagination');
            body.innerHTML = '<tr><td colspan=""4"" style=""text-align:center; padding:40px; opacity:0.5;"">' + 
                             (currentLang === 'fr' ? '⌛ Chargement...' : '⌛ Loading...') + '</td></tr>';
            pagin.style.display = 'none';
            fullHistory = [];
            itemsShown = 0;

            try {
            	const url = apiBaseUrl ? `${apiBaseUrl}/api/history` : '/api/history';
                const res = await fetch(url);
                fullHistory = await res.json();
                body.innerHTML = '';
                renderMoreHistory();
            } catch (err) {
                body.innerHTML = '<tr><td colspan=""4"" style=""text-align:center; color:var(--danger)"">Error loading history</td></tr>';
            }
        }

        function renderMoreHistory() {
            const body = document.getElementById('historyBody');
            const pagin = document.getElementById('historyPagination');
            const nextBatch = fullHistory.slice(itemsShown, itemsShown + ITEMS_PER_PAGE);
            
            nextBatch.forEach(entry => {
                const row = document.createElement('tr');
                let dateStr = entry.startTime || """";
                if (dateStr.length > 16) {
                    dateStr = dateStr.substring(0, 10) + "" "" + dateStr.substring(11, 16);
                }
                row.innerHTML = 
                    '<td><span class=""history-system"">' + entry.system + '</span></td>' +
                    '<td>' + entry.gameTitle + '</td>' +
                    '<td style=""color:var(--primary)"">' + entry.duration + '</td>' +
                    '<td style=""opacity:0.6"">' + dateStr + '</td>';
                body.appendChild(row);
            });

            itemsShown += nextBatch.length;
            pagin.style.display = itemsShown < fullHistory.length ? 'block' : 'none';
        }

        async function performAction(action, data = {}) {
            try {
            	const url = apiBaseUrl ? `${apiBaseUrl}/api/action` : '/api/action';
                const response = await fetch(url, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ action, password: operatorPassword, ...data })
                });
                
                if (response.status === 401) {
                    alert(translations[currentLang].passWrong);
                    sessionStorage.removeItem('op_pass');
                    location.reload();
                    return;
                }
                
                updateStatus();
            } catch (e) {
                alert(translations[currentLang].apiError);
            }
        }

        function toggleLauncherBannerDetails() {
            const details = document.getElementById('bannerDetailsList');
            const btn = document.getElementById('btnToggleBanner');
            if (!details || !btn) return;
            
            const isVisible = details.style.display === 'flex';
            details.style.display = isVisible ? 'none' : 'flex';
            btn.classList.toggle('active', !isVisible);
        }

        async function stopGameOnSpecificMachine(mVal, mName) {
            const label = currentLang === 'fr' ? `Arrêter le jeu sur ${mName} ?` : `Stop game on ${mName}?`;
            if (!confirm(label)) return;

            try {
                const url = mVal === 'local' ? '/api/action' : `/api/relay?target=${mVal}&path=/api/action`;
                const payload = { action: 'stop_game', password: operatorPassword };
                
                const r = await fetch(url, { 
                    method: 'POST', 
                    headers: { 'Content-Type': 'application/json' }, 
                    body: JSON.stringify(payload) 
                });
                
                if (!r.ok && r.status === 401) {
                    const np = prompt(currentLang === 'fr' ? 'Mot de passe opérateur requis :' : 'Operator password required:');
                    if (np) { operatorPassword = np; sessionStorage.setItem('op_pass', np); stopGameOnSpecificMachine(mVal, mName); }
                }
            } catch(e) { console.error(""Stop specific game error"", e); }
        }

        function updateLauncherBanner() {
            const banner = document.getElementById('launcherCurrentGameBanner');
            const detailsList = document.getElementById('bannerDetailsList');
            const toggleBtn = document.getElementById('btnToggleBanner');
            if (!banner || !detailsList || !toggleBtn) return;
            
            let selectedVals = Array.from(document.querySelectorAll('input[name=""targetMachines""]:checked')).map(i => i.value);
            if (selectedVals.length === 0) selectedVals = ['local'];

            // EN: Get all playing machines among selected / FR: Récupérer toutes les machines qui jouent parmi les sélectionnées
            const playingMachines = [];
            selectedVals.forEach(val => {
                Object.values(_machines).forEach(m => {
                    const matchVal = m.isLocal ? 'local' : m.name;
                    if (matchVal === val && m.currentGameName && m.currentGameName !== 'Idle / Menu') {
                        playingMachines.push(m);
                    }
                });
            });

            if (playingMachines.length === 0) {
                 banner.style.display = 'none';
                 return;
            }

            banner.style.display = 'flex';
            
            // EN: Update Summary / FR: Mise à jour du résumé
            if (playingMachines.length === 1) {
                const m = playingMachines[0];
                document.getElementById('launcherBannerSystem').innerText = m.currentGameSystem || '';
                document.getElementById('launcherBannerGame').innerText = m.currentGameName || '';
                toggleBtn.style.display = 'none';
            } else {
                // EN: Check if all playing the same game / FR: Vérifier si tous jouent au même jeu
                const firstGame = playingMachines[0].currentGameName;
                const allSame = playingMachines.every(m => m.currentGameName === firstGame);
                
                if (allSame) {
                    document.getElementById('launcherBannerSystem').innerText = `${playingMachines.length} BORNES`;
                    document.getElementById('launcherBannerGame').innerText = firstGame;
                } else {
                    document.getElementById('launcherBannerSystem').innerText = ""MULTI-JEUX"";
                    document.getElementById('launcherBannerGame').innerText = `${playingMachines.length} ` + (currentLang === 'fr' ? 'bornes actives' : 'active machines');
                }
                toggleBtn.style.display = 'flex';
            }

            // EN: Rebuild detailed list / FR: Reconstruire la liste détaillée
            detailsList.innerHTML = '';
            playingMachines.forEach(m => {
                const mVal = m.isLocal ? 'local' : m.name;
                const row = document.createElement('div');
                row.className = 'banner-machine-row';
                row.innerHTML = `
                    <div class=""banner-machine-info"">
                        <div class=""banner-machine-name"">${m.name}</div>
                        <div class=""banner-machine-game"">${m.currentGameName}</div>
                    </div>
                    <button class=""btn-danger"" style=""padding:4px 8px; font-size:0.7rem;"" onclick=""stopGameOnSpecificMachine('${mVal}', '${m.name.replace(/'/g, ""\\"")}')"">🛑</button>
                `;
                detailsList.appendChild(row);
            });
        }

        async function stopCurrentGameFromWeb() {
            let selectedMachines = Array.from(document.querySelectorAll('input[name=""targetMachines""]:checked')).map(i => i.value);
            if (selectedMachines.length === 0) selectedMachines = ['local'];
            
            if (!confirm(""Êtes-vous sûr de vouloir arrêter le jeu sur les terminaux sélectionnés ?"")) return;

            for(let j = 0; j < selectedMachines.length; j++) {
                const mVal = selectedMachines[j];
                try {
                    const url = mVal === 'local' ? '/api/action' : `/api/relay?target=${mVal}&path=/api/action`;
                    const payload = { action: 'stop_game', password: operatorPassword };
                    
                    const r = await fetch(url, { 
                        method: 'POST', 
                        headers: { 'Content-Type': 'application/json' }, 
                        body: JSON.stringify(payload) 
                    });
                    
                    if (!r.ok && r.status === 401) {
                        const np = prompt(currentLang === 'fr' ? 'Mot de passe opérateur requis :' : 'Operator password required:');
                        if (np) { operatorPassword = np; sessionStorage.setItem('op_pass', np); j--; }
                    }
                } catch(e) { console.error(""Stop game error for"", mVal, e); }
            }
        }

        function confirmLock() {
            if (confirm(translations[currentLang].confirmLock)) {
                performAction('lock');
            }
        }

        function confirmFreePlay() {
            if (confirm(translations[currentLang].confirmFreePlay)) {
                performAction('toggle_freeplay');
            }
        }

        function confirmOperator() {
            if (confirm(translations[currentLang].confirmOperator)) {
                performAction('open_local_operator');
            }
        }

        async function sendOpMessage() {
            const msgInput = document.getElementById('opMessage');
            const msg = msgInput.value;
            const dur = parseInt(document.getElementById('msgDuration').value);
            if (msg) {
                await performAction('show_message', { message: msg, duration: dur });
                msgInput.value = '';
            }
        }

        function setDuration() {
            const mins = parseInt(document.getElementById('sessionMins').value);
            if (!isNaN(mins)) {
                performAction('set_duration', { minutes: mins });
            }
        }

        function setMPC() {
            const mins = parseInt(document.getElementById('mpcInput').value);
            if (!isNaN(mins)) {
                performAction('set_minutes_per_credit', { minutes: mins });
            }
        }

        function setPublicIp() {
            const ip = document.getElementById('publicIpInput').value;
            performAction('set_public_ip', { publicIp: ip });
        }

        document.getElementById('opMessage').addEventListener('keypress', (e) => {
            if (e.key === 'Enter') sendOpMessage();
        });

        document.getElementById('passInput').addEventListener('keypress', (e) => {
            if (e.key === 'Enter') login();
        });

        let networkCounter = 0;
        setInterval(() => {
            if (isHomeView) {
                updateNetwork();
            } else {
                updateStatus();
                // EN: Sync network state every 3 seconds even in launcher to update ""Playing Now"" badges
                // FR: Synchroniser le réseau toutes les 3s même dans le lanceur pour les badges ""En cours""
                networkCounter++;
                if (networkCounter >= 3) {
                    updateNetwork().then(() => {
                        refreshPlayingBadges();
                    });
                    networkCounter = 0;
                }
            }
        }, 1000);

        // EN: Show the users management view / FR: Afficher la vue de gestion des utilisateurs
        function showUsers() {
            if (!operatorPassword) {
                document.getElementById('loginOverlay').style.display = 'flex';
                // FR: On affiche le message pour dire que c'est requis pour cette section
                const t = translations[currentLang];
                alert(currentLang === 'fr' ? 'Mot de passe opérateur requis pour gérer les utilisateurs.' : 'Operator password required to manage users.');
                return;
            }
            document.getElementById('home-view').style.display = 'none';
            document.getElementById('mainContent').style.display = 'none';
            document.getElementById('launcher-view').style.display = 'none';
            document.getElementById('users-view').style.display = 'block';
            loadUsers();
        }

        // EN: Load users list from API / FR: Charger la liste des utilisateurs depuis l'API
        function loadUsers() {
            fetch('/api/admin/users', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ password: operatorPassword })
            })
            .then(r => r.json())
            .then(users => {
                const tbody = document.getElementById('usersBody');
                const t = translations[currentLang] || {};
                if (!users || users.length === 0) {
                    tbody.innerHTML = '<tr><td colspan=""4"" style=""text-align:center; opacity:0.6;"">' + (currentLang === 'fr' ? 'Aucun utilisateur enregistré' : 'No registered users') + '</td></tr>';
                    return;
                }
                tbody.innerHTML = '';
                users.forEach(u => {
                    const tr = document.createElement('tr');
                    // EN: Status badge / FR: Badge de statut
                    let statusBadge = '';
                    const statusLower = (u.status || 'pending').toLowerCase();
                    if (statusLower === 'approved') {
                        statusBadge = '<span style=""background:var(--success); color:white; padding:3px 8px; border-radius:8px; font-size:0.8em;"">✅ Approved</span>';
                    } else if (statusLower === 'rejected') {
                        statusBadge = '<span style=""background:var(--danger); color:white; padding:3px 8px; border-radius:8px; font-size:0.8em;"">❌ Rejected</span>';
                    } else {
                        statusBadge = '<span style=""background:#f39c12; color:white; padding:3px 8px; border-radius:8px; font-size:0.8em;"">⏳ Pending</span>';
                    }
                    // EN: Format date / FR: Formater la date
                    const dateStr = u.createdAt ? new Date(u.createdAt).toLocaleDateString() : '—';
                    // EN: Action buttons / FR: Boutons d'action
                    let actions = '';
                    if (statusLower !== 'approved') {
                        actions += `<button onclick=""approveUser('${u.id}')"" style=""background:var(--success); color:white; border:none; padding:5px 10px; border-radius:6px; cursor:pointer; margin:2px; font-size:0.85em;"" title=""Approve"">✅</button>`;
                    }
                    if (statusLower !== 'rejected') {
                        actions += `<button onclick=""rejectUser('${u.id}')"" style=""background:var(--danger); color:white; border:none; padding:5px 10px; border-radius:6px; cursor:pointer; margin:2px; font-size:0.85em;"" title=""Reject"">❌</button>`;
                    }
                    tr.innerHTML = `<td style=""font-weight:bold;"">${u.username}</td><td>${statusBadge}</td><td style=""opacity:0.7;"">${dateStr}</td><td style=""text-align:right;"">${actions}</td>`;
                    tbody.appendChild(tr);
                });
            })
            .catch(err => {
                console.error('Error loading users:', err);
                document.getElementById('usersBody').innerHTML = '<tr><td colspan=""4"" style=""color:var(--danger);"">' + (currentLang === 'fr' ? 'Erreur de chargement' : 'Loading error') + '</td></tr>';
            });
        }

        // EN: Approve a user / FR: Approuver un utilisateur
        function approveUser(userId) {
            fetch('/api/admin/users/action', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ userId: userId, action: 'approve', password: operatorPassword })
            })
            .then(r => { if (r.ok) loadUsers(); else alert(translations[currentLang].apiError || 'Error'); })
            .catch(err => console.error('Approve error:', err));
        }

        // EN: Reject a user / FR: Rejeter un utilisateur
        function rejectUser(userId) {
            fetch('/api/admin/users/action', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ userId: userId, action: 'reject', password: operatorPassword })
            })
            .then(r => { if (r.ok) loadUsers(); else alert(translations[currentLang].apiError || 'Error'); })
            .catch(err => console.error('Reject error:', err));
        }

        // EN: Update pending users badge on home / FR: Mettre à jour le badge des utilisateurs en attente
        function updatePendingBadge() {
            if (!operatorPassword) return;
            fetch('/api/admin/users', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ password: operatorPassword })
            })
            .then(r => r.ok ? r.json() : [])
            .then(users => {
                const pending = (users || []).filter(u => (u.status || '').toLowerCase() === 'pending').length;
                const badge = document.getElementById('pendingUsersBadge');
                if (badge) {
                    if (pending > 0) {
                        badge.textContent = pending;
                        badge.style.display = 'inline';
                    } else {
                        badge.style.display = 'none';
                    }
                }
            })
            .catch(() => {});
        }

        // EN: Periodically update pending badge / FR: Mise à jour périodique du badge en attente
        // [BATRUN-FORK] Handle Secure Fullscreen Exit (Long Press Escape)
        function updateEscOverlay(elapsed) {
            const overlay = document.getElementById('escOverlay');
            const circle = document.getElementById('escCircle');
            const txtSec = document.getElementById('escSec');
            const t = publicTranslations[currentLang];

            if (elapsed < ESC_OVERLAY_DELAY) {
                overlay.style.display = 'none';
                return;
            }

            overlay.style.display = 'flex';
            overlay.style.opacity = 1;
            
            const total = ESC_HOLD_DURATION;
            const progress = (elapsed / total);
            // Circle math: stroke-dashoffset = radius * 2 * PI * (1 - progress)
            const radius = 70;
            const circumference = 2 * Math.PI * radius;
            const offset = circumference * (1 - progress);
            circle.style.strokeDashoffset = offset || 0;
            circle.style.strokeDasharray = circumference;

            const remaining = Math.max(0, Math.ceil((total - elapsed) / 1000));
            txtSec.innerText = remaining + 's';
            const msgEl = document.getElementById('escMsg');
            if (msgEl) msgEl.innerText = t.escHold;
        }

        window.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && document.fullscreenElement && isStreaming) {
                // [BATRUN] If keyboard lock is not supported or failed, ESC will exit immediately anyway.
                // But we use 'capture: true' to try to see the event first.
                if (_escHoldStart === 0) {
                    _escHoldStart = performance.now();
                    if (_escHoldInterval) clearInterval(_escHoldInterval);
                    _escHoldInterval = setInterval(() => {
                        const elapsed = performance.now() - _escHoldStart;
                        updateEscOverlay(elapsed);
                        if (elapsed >= ESC_HOLD_DURATION) {
                            clearInterval(_escHoldInterval);
                            _escHoldInterval = null;
                            _escHoldStart = 0;
                            const overlay = document.getElementById('escOverlay');
                            if (overlay) overlay.style.display = 'none';
                            if (document.exitFullscreen) document.exitFullscreen();
                        }
                    }, 50);
                }
            }
        }, true); // EN: Use capture phase to intercept before iframe / FR: Utiliser la phase de capture

        window.addEventListener('keyup', (e) => {
            if (e.key === 'Escape') {
                if (_escHoldInterval) clearInterval(_escHoldInterval);
                _escHoldInterval = null;
                _escHoldStart = 0;
                const overlay = document.getElementById('escOverlay');
                if (overlay) overlay.style.display = 'none';
            }
        }, true);

        // EN: Show the security dashboard / FR: Afficher le tableau de bord de sécurité
        function showSecurity() {
            if (!operatorPassword) {
                document.getElementById('loginOverlay').style.display = 'flex';
                alert(currentLang === 'fr' ? 'Mot de passe opérateur requis pour la sécurité.' : 'Operator password required for security.');
                return;
            }
            toggleSecurity();
        }

        let securityRefreshTimer = null;

        function toggleSecurity() {
            const modal = document.getElementById('securityModal');
            if (modal.style.display === 'block') {
                modal.style.display = 'none';
                if (securityRefreshTimer) clearInterval(securityRefreshTimer);
            } else {
                modal.style.display = 'block';
                refreshSecurity();
                
                if (securityRefreshTimer) clearInterval(securityRefreshTimer);
                securityRefreshTimer = setInterval(() => {
                    const sel = document.getElementById('logDateSelect');
                    if (sel && sel.value === '') loadSecurityLogs();
                }, 5000);
            }
        }

        async function refreshSecurity() {
            await loadSecurityDates();
            const sel = document.getElementById('logDateSelect');
            await loadSecurityLogs(sel ? sel.value : null);
        }

        async function loadSecurityDates() {
            try {
                console.log('[Security] Loading log dates...');
                const res = await fetch('/api/admin/security/dates', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ password: operatorPassword })
                });
                if (!res.ok) throw new Error('HTTP ' + res.status);
                
                const dates = await res.json();
                console.log('[Security] Dates received:', dates);
                
                const select = document.getElementById('logDateSelect');
                if (!select) return;

                const currentVal = select.value;
                
                // Clear all except first (Live)
                while (select.options.length > 1) select.remove(1);
                
                if (Array.isArray(dates)) {
                    dates.forEach(d => {
                        const opt = document.createElement('option');
                        opt.value = d;
                        opt.innerText = d;
                        select.appendChild(opt);
                    });
                } else {
                    console.error('[Security] Invalid dates format received:', dates);
                }
                
                if (currentVal && dates.includes && dates.includes(currentVal)) select.value = currentVal;
            } catch(e) {
                console.error('[Security] Failed to load log dates:', e);
            }
        }

        async function loadSecurityLogs(date = null) {
            const logList = document.getElementById('securityLogList');
            const blockedList = document.getElementById('blockedIpList');
            if (!logList || !blockedList) return;

            try {
                const res = await fetch('/api/admin/security/logs', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ password: operatorPassword, date: date || '' })
                });
                const data = await res.json();
                
                // Render Logs
                logList.innerHTML = '';
                if (data.logs && data.logs.length > 0) {
                    data.logs.forEach(l => {
                        const div = document.createElement('div');
                        div.style = 'padding:8px; border-bottom:1px solid rgba(255,255,255,0.05);';
                        const typeColor = l.type === 0 ? 'var(--success)' : 'var(--danger)';
                        const typeLabel = l.type === 0 ? 'SUCCESS' : (l.type === 1 ? 'FAILURE' : (l.type === 2 ? 'BANNED' : 'DROPPED'));
                        
                        div.innerHTML = `
                            <div style=""display:flex; justify-content:space-between; margin-bottom:4px;"">
                                <span style=""color:${typeColor}; font-weight:bold;"">[${typeLabel}]</span>
                                <span style=""opacity:0.6; font-size:0.75rem;"">${l.timestamp ? new Date(l.timestamp).toLocaleString() : '---'}</span>
                            </div>
                            <div style=""overflow:hidden; text-overflow:ellipsis; white-space:nowrap;""><strong>IP:</strong> ${l.ip} | <strong>User:</strong> ${l.username}</div>
                            <div style=""opacity:0.7; font-size:0.8rem; margin-top:2px;"">${l.details}</div>
                        `;
                        logList.appendChild(div);
                    });
                } else {
                    logList.innerHTML = `<div style=""padding:20px; text-align:center; opacity:0.5;"">${translations[currentLang].noSecurityLogs}</div>`;
                }

                // Render Blocked IPs
                blockedList.innerHTML = '';
                const blockedKeys = Object.keys(data.blocked || {});
                if (blockedKeys.length > 0) {
                    blockedKeys.forEach(ip => {
                        const expiry = new Date(data.blocked[ip]);
                        const div = document.createElement('div');
                        div.style = 'padding:8px; border-bottom:1px solid rgba(255,255,255,0.05); display:flex; justify-content:space-between; align-items:center;';
                        div.innerHTML = `
                            <div><strong>${ip}</strong></div>
                            <div style=""text-align:right;"">
                                <div style=""font-size:0.7rem; opacity:0.6;"">EXPIRE LE:</div>
                                <div style=""color:var(--danger); font-weight:bold;"">${expiry.toLocaleString()}</div>
                            </div>
                        `;
                        blockedList.appendChild(div);
                    });
                } else {
                    blockedList.innerHTML = `<div style=""padding:20px; text-align:center; opacity:0.5;"">${translations[currentLang].noBlockedIps}</div>`;
                }
            } catch(e) { console.error('Error loading security logs:', e); }
        }

        // EN: Apply stored language on page load / FR: Appliquer la langue sauvegardee au chargement
        setLanguage(currentLang);

    </script>
</body>
</html>";
        }
    }
}