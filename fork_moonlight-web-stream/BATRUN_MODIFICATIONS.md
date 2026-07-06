# BatRun Modifications — moonlight-web-stream

Ce fichier documente toutes les modifications apportées par BatRun au projet
[moonlight-web-stream](https://github.com/Myzel394/moonlight-web-stream).

## Comment identifier les modifications

Tout le code modifié par BatRun est marqué avec le commentaire `[BATRUN-FORK]`.

## Fichiers modifiés

| Fichier | Nature de la modification | Risque Upstream |
|---------|--------------------------|-----------------|
| `static/stream.js` (compilé depuis `web/stream.ts`) | Mode kiosque arcade (`arcadeMode` URL param) | FAIBLE — logique additive |
| `static/index.js` (compilé depuis `web/index.ts`) | Suppression ouverture nouvel onglet | FAIBLE — logique additive |
| `web/stream/audio/audio_context_base.ts` | Déblocage de l'AudioContext suspendu (User Interaction) | FAIBLE — logique additive |
| `web/stream/audio/audio_element.ts` | Retrait du mute par défaut (`muted = false`) | FAIBLE — paramétrage |
| `web/stream/video/video_element.ts` | Retrait du mute par défaut (`muted = false`) | FAIBLE — paramétrage |
| `web/stream.html` | Masquage de la sidebar native Moonlight (`#sidebar-root`) | FAIBLE — attribut HTML inline |
| `Core/ArcadeApiService.cs` | Intégration clavier virtuel (VK) Login & Polling Manette | MOYEN — logique backend/frontend mixée |

## Modifications détaillées

### `web/stream.ts` — Mode Arcade

Ajout du paramètre URL `arcadeMode=1` qui :
- Désactive la barre latérale (sidebar) pendant le streaming
- Active le plein écran automatique **sans prompt**
- Masque le dialogue de connexion (ConnectionInfoModal) — auto-close à connexion établie

```typescript
// [BATRUN-FORK]: Arcade/kiosk mode — disable UI chrome when arcadeMode=1
const arcadeMode = new URLSearchParams(location.search).get('arcadeMode') === '1'
```

### `web/index.ts` — Suppression Nouvel Onglet

Le comportement par défaut ouvre `stream.html` dans un nouvel onglet.
En mode BatRun, le stream est intégré dans une `<iframe>` de la page publique.

```typescript
// [BATRUN-FORK]: Do not open stream in new tab; BatRun handles the iframe embedding
```

### Audio & Vidéo (`audio_context_base.ts`, `audio_element.ts`, `video_element.ts`)

Pour permettre à la borne d'arcade de diffuser le son immédiatement au lancement sans exiger le clic de souris (Gamepad pur), le mute par défaut du lecteur HTML5 est désactivé. De plus, `audioContext.resume()` est appelé explicitement pour réveiller le contexte audio si ce dernier a été créé par un processus background du navigateur en état `suspended`.

```typescript
// [BATRUN-FORK]: Do not mute by default, respect browser's whitelist bypass for Arcade.
this.audioElement.muted = false;
this.videoElement.muted = false;

// [BATRUN-FORK]: Resume AudioContext if suspended (created outside a user gesture context)
if (this.audioContext?.state === 'suspended') {
    this.audioContext.resume().catch(e => console.warn('[BatRun] AudioContext resume failed:', e))
}
```

### `web/stream.html` — Masquage de la Sidebar Moonlight

La sidebar Moonlight est un panel latéral contenant un bouton flèche (`→`) permettant d'accéder aux réglages du stream. Dans le contexte BatRun arcade, toute l'interface est gérée par `ArcadeApiService.cs` et ce panel est inutile et gênant visuellement.

L'élément `#sidebar-root` a été masqué directement via un attribut `style` inline pour garantir son masquage même si le script Moonlight essaie de le rendre visible dynamiquement :

```html
<!-- [BATRUN] Hidden: UI is managed by BatRun arcade interface -->
<div class="sidebar-overlay prevent-start-transition" id="sidebar-root"
     style="display:none !important; visibility:hidden !important;">
```

> **Note de maintenance** : Lors d'une mise à jour upstream de `stream.html`, vérifier que cette modification est réappliquée sur l'élément `#sidebar-root`.

## Procédure de mise à jour Upstream

1. Observer les nouveaux commits/PR upstream : https://github.com/Myzel394/moonlight-web-stream
2. Vérifier les fichiers modifiés listés ci-dessus pour détecter les conflits potentiels
3. Pour chaque conflit, rechercher `[BATRUN-FORK]` dans le fichier et réappliquer le patch
4. Compiler **uniquement le TypeScript Front-End** : exécuter `npm install` puis `npm run build-light` dans le dossier `moonlight-web-stream-master`. Ne pas faire `npm run build` total car cela requiert des dépendances de compilation C++ / Rust complexes pour le Backend qui n'est pas utilisé tel quel.
5. Copier le résultat compilé (`dist/`) dans la destination de BatRun (géré automatiquement par `dotnet build`).

## Modifications Spécifiques BatRun (Backend Interface)

### `Core/ArcadeApiService.cs` — Public Interface & Gamepad UX

#### Clavier Virtuel (VK) pour l'Authentification
Ajout d'un clavier virtuel sur la page de login publique (`/arcade`) pour permettre la saisie du nom d'utilisateur et du mot de passe exclusivement à la manette.
- Le clavier s'ouvre avec la touche **X** (Button 2) ou **Start** (Button 9).
- Validation automatique du formulaire lors de l'appui sur **"OK"** si le focus est sur un champ d'identification.
- Gestion dynamique de l'AZERTY/QWERTY selon la détection de langue.

#### Gestion du "Button Bleed"
Correction d'un problème où le bouton **Start** maintenu pour lancer un jeu était détecté comme un appui dans l'interface de liste de jeux après le retour du stream.
- Injection d'un reset d'état (`_gpActive = false`, `_gpLastButtons = {}`) lors de l'appel à `showMainUI()`.
- Désactivation forcée du polling pendant les transitions d'état.

#### Permissions Iframe
Ajout de `gamepad` et `autoplay` dans la `Permissions-Policy` de l'iframe Moonlight pour assurer que les contrôleurs sont détectés immédiatement sans interaction souris.

## Builds

Les fichiers dans `moonlight-web-stream/static/` sont des versions compilées des sources
du dossier `fork_moonlight-web-stream/moonlight-web-stream-master/web/`.
Le binaire `web-server.exe` est issu des releases officielles GitHub et n'est PAS modifié.
