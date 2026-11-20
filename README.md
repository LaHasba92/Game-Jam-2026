# Game Jam 2026
# CanonPOW

> **Un tir. Combien de murs ?**

Jeu mobile hypercasual en 3D, fait en 48 h pour la **Game Jam Voodoo x Gaming Campus** (novembre 2026),
sur le thème *physics-based destruction puzzle*.

On tend la fronde, on règle l'angle et la puissance, on lâche : le boulet traverse les murs un par un.
Chaque mur cassé fait apparaître le suivant un peu plus loin. Le but : **casser le plus de murs possible en un tir**.
Les pièces gagnées paient les améliorations, et la partie suivante va un mur plus loin.

---

## Jouer

| Étape | Téléphone | PC (build du jury) |
|---|---|---|
| Lancer une partie | **START** au menu | clic sur **START** |
| Viser | poser le doigt dans le bas de l'écran et glisser : gauche/droite = direction, **tirer vers le bas = monter l'angle** | clic maintenu + déplacement de la souris |
| Tirer | relâcher quand la jauge (à gauche) est dans le **vert** (PERFECT ×1,25) | relâcher le clic |
| Diriger en vol | glisser le doigt à gauche ou à droite | clic maintenu + souris, ou flèches / A-D |
| Passer la fin du vol | bouton « Passer » après 5 s | **Espace** |
| Après la partie | **REJOUER**, **BOUTIQUE** ou **Claim X2** (gain doublé) | idem |

## La boucle de jeu

1. **Viser et tirer** : angle (limité à 30° au départ), direction et jauge de puissance.
2. **Voler** : le boulet rebondit au sol ; trampolines, boosters et ballons l'aident, les flaques le freinent.
3. **Casser** : un mur casse à tout impact en l'air et explose en morceaux ; le boulet garde une partie de sa vitesse (Élan) et le mur suivant apparaît 1,15 fois plus loin.
4. **Fin** : le boulet arrive en roulant sur un mur, ou s'arrête.
5. **Gagner** : 0,5 pièce par mètre × Argent, + un bonus par mur (10, 20, 30…). Claim X2 double le gain.
6. **Améliorer** dans la boutique, puis rejouer.

| Amélioration | Effet | Niveau 1 |
|---|---|---|
| Puissance | vitesse de départ | 25 m/s, ×1,06 par niveau |
| Aérodynamisme | frottement de l'air | 0,10, ×0,95 par niveau |
| Élan | vitesse gardée après un mur cassé | 60 %, +3 % par niveau (85 % max) |
| Rebond | hauteur des rebonds | 0,45, +0,015 par niveau |
| Argent | multiplicateur de pièces | ×1, +0,1 par niveau |

Le détail des règles et des chiffres est dans le **GDD** de l'équipe.

---

## Ouvrir le projet

- **Unity 6000.3.21f1** (Unity 6.3 LTS), rendu **URP**, Input System actif (mode *Both*).
- Ouvrir le dossier du dépôt dans Unity Hub (**Add → Add project from disk**).
- Scène de jeu : **`Assets/_Project/Scenes/Game.unity`** (la scène `Boot` charge la sauvegarde puis ouvre `Game`).
- Build : Android en portrait 9:16, et PC (fenêtre portrait 540 × 960) pour le jury.

## Organisation du code

Une seule scène de jeu, des scripts petits qui ne se connaissent pas : ils communiquent par **événements**
(`GameEvents`) et suivent les **contrats** écrits au début de la jam (`Contracts.cs`).
Axes : **X** = la piste, **Y** = la hauteur, **Z** = gauche/droite ; le lanceur est en x = 0.

```
Accueil ──START──> Visée ──lâcher──> Vol ──fin──> Résultat ──REJOUER──> Visée
   ^                                                  │
   └──────────── BOUTIQUE / Retour ───────────────────┘
```

| Événement | Émis par | Sert à |
|---|---|---|
| `OnStateChanged` | GameManager | afficher le bon écran, préparer une nouvelle partie |
| `OnShotFired` | LauncherController | passer en Vol, faire rebondir la fronde |
| `OnWallHit`, `OnWallBroken` | WallController, LevelManager | compter les murs, « POW ! » / « COMBO x3 », ralenti |
| `OnLevelLoaded` | LevelManager | un nouveau mur est placé |
| `OnRunEnded` | ProjectileController | écran de fin, pièces, meilleur score |
| `OnCoinsChanged` | RunRewardManager, boutique | mettre à jour les prix et les boutons |

### Les scripts principaux

| Dossier | Scripts | Rôle |
|---|---|---|
| `_Project/Scripts/Core` | `GameManager`, `GameEvents`, `Contracts`, `Boot` | machine à états, événements, contrats |
| `Scripts/Launcher` | `LauncherController`, `ProjectileController`, `BallPhysics`, `CameraDirector` | visée + jauge + pointillés, vol calculé (gravité, frottement, rebonds, couloir), pilotage, caméra |
| `Scripts/Wall` | `WallController`, `WallRules`, `WallVisualSet`, `LevelManager`, `LevelDefinition` | murs enchaînés à l'infini, casse, explosion en morceaux (pool) |
| `Scripts/World` | `WorldStreamer`, `GroundObject` | sol infini en tronçons de 50 m recyclés, objets de la piste |
| `Scripts/Upgrade(s)` | `UpgradeApplier`, `UpgradeShopButton`, `RunRewardManager` | améliorations appliquées au tir, achats en boutique, pièces de fin de partie |
| `nathan_script`, racine `Assets` | `UpgradeDefinition`, `LauncherDefinition`, `UpgradeSaveData`, `EconomyManager` | fiches d'amélioration, lanceurs, sauvegarde JSON |
| `_Project/Scripts/UI`, `script` | `UIStateSwitcher`, `UIButtonAction`, `score_manager` | écrans, boutons des prefabs, meilleur score |
| `_Project/Scripts/Feedback` | `FeedbackManager` | arrêt sur image, ralenti, textes « POW ! », vibration |

Le gameplay tient dans un seul prefab, **`_Project/Prefabs/Gameplay`** (mur, sol, boulet, lanceur, feedback,
améliorations) ; les écrans de Hugo sont dans `_Project/Prefabs/UI`. Toutes les valeurs de réglage sont dans
l'Inspector ou dans des ScriptableObjects : on équilibre sans toucher au code.

### Choix techniques

- **Vol calculé par notre code** (et non par le Rigidbody) : pas de traversée du sol à grande vitesse, bouton « Passer » qui calcule la fin du vol d'un coup, et les **pointillés de visée utilisent exactement la même physique** que le vol (`BallPhysics`).
- **Pools** pour les murs, les morceaux d'explosion, les tronçons de sol et les objets : rien n'est créé ni détruit pendant une partie (60 images/s visées sur mobile).
- **Explosion identique sur tous les murs** : vitesses données directement aux morceaux, avec une graine aléatoire fixe.

## Outils (menu **CanonPOW** dans Unity)

| Commande | Effet |
|---|---|
| Créer les 10 niveaux | crée les 10 premiers murs (`LevelDefinition`) avec les valeurs du GDD |
| Créer le mur greybox | prefab de mur : intact + morceaux pour l'explosion |
| Créer le sol et les objets greybox | tronçon de sol et objets de la piste |
| Réinitialiser la sauvegarde | remet à zéro améliorations, pièces et meilleur score (avant un playtest ou la vidéo) |
| Trouver les scripts manquants | liste les objets de la scène dont un script a disparu |

Et sur le composant **BalanceTester** : clic droit → **Simuler 1000 parties**. Le simulateur rejoue la physique
sans scène et donne, par profil de joueur, le nombre moyen de murs cassés. Il nous a servi à choisir l'écart
entre les murs (×1,15 : environ 2 murs au début, 15 avec tout amélioré).

## Équipe

| Membre | Pôle | Participation |
|---|---|---|
| **Maceo CHALONS** | Business 2 : Product manager | vision produit, premier GDD, monétisation et marketing |
| **Benjamin BODIN** | Tech : boucle principale | GameManager et événements (`GameEvents`, contrats), implémentation des assets, synchronisation des fonctionnalités dans la scène Game, fusions et builds |
| **Sami BOUGRINI** | Tech 1 : physique, mécaniques, debug | murs enchaînés et explosion, sol infini et objets de la piste, lanceur, boulet et caméra (repris en cours de jam), feedback (POW, ralenti), branchement des pièces, de la boutique et des améliorations, outils d'éditeur et simulateur d'équilibrage, debug |
| **Andy DONERO** | Tech : physique et mécaniques | lanceur et vol au départ (GDD) ; partie reprise par Sami pendant la jam |
| **Nathan BAURIN** | Tech : systèmes d'amélioration | améliorations (fiches, coûts, valeurs), lanceurs, économie, sauvegarde, pubs récompensées |
| **Hugo HEYSER** | Tech / design des menus | interface : menu, boutique, écran de fin, boutons |
| **Louis VAN DEN BOSSCHE** | Assets 3D | environnement : terrain, décors, murs |
| **Killian MAHINE** | Assets 3D | objets et lanceurs : fronde, canon, objets de la piste |

## Utilisation de l'IA

L'équipe a utilisé un assistant IA (Claude) comme **tuteur et outil de prototypage** : explications de Unity
et du C#, propositions de code relues, testées et adaptées dans Unity par chacun, simulation pour
l'équilibrage, et rédaction de documents (GDD, ce README). Les décisions de design (jeu sans cible, couloir,
pilotage, améliorations) ont été prises par l'équipe en jouant.

## Crédits

Modèles et textures : réalisés par l'équipe et packs d'assets libres de l'Asset Store (dossiers `Monster`,
`Fantasy Forest Environment`, `Objets`, `Testure`). Police et sons : sources libres (Kenney, freesound en CC0).
