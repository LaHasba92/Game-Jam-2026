# Extension du prototype CanonPOW

## Prise en main

Les cinq ajouts se jouent dans `Game`. La fronde et le canon peuvent faire perdre un tir, le Mur géant conserve les dégâts entre les parties et la boutique déclenche une transition au premier achat du canon. Les plans publicitaires réutilisent ces mécanismes.

### Ce qui reste à Nathan

1. **Dans la boutique**, essayer le canon avant le mur 8, avec 14 999 pièces, puis avec 15 000 pièces et le mur 8 atteint. Seul le dernier essai doit débiter le portefeuille et lancer la transition. Rééquiper le canon doit rester gratuit.
2. **Sur le Mur géant**, faire deux impacts, quitter complètement le jeu puis le rouvrir. Comparer le solde, le record, le lanceur équipé et les PV. Refaire un plan en capture à 2 % et vérifier que les quatre valeurs du joueur reviennent à la sortie.
3. **Avec Sami**, relever la vitesse réelle à l’arrivée sur trois tirs qui touchent le boss. Ajuster les PV ou les dégâts par m/s en fonction du nombre de parties réellement nécessaires. Remettre les valeurs et les résultats PC/Android à Benji avant le gel.

### Installer sur une branche de test

1. Dans GitHub Desktop, choisir le dépôt `Game-Jam-2026`. Faire **Fetch origin**, sélectionner `integration/codex` puis **Branch → New branch**. Le patch a été produit sur `5f3d732`. Garder le contenu de l’archive décompressée en dehors du projet.
2. Dans **Repository → Open in Terminal**, lancer `git apply --check "C:/chemin/CanonPOW-extension.patch"`. Remplacer le chemin par celui du patch extrait. Si la vérification réussit, relancer la commande en retirant `--check`. En cas de divergence, Benji peut fusionner les changements à partir de la base indiquée. Le dossier `Sources` contient les fichiers complets pour la revue.
3. Ouvrir cette copie dans Unity **6000.3.29f1**. Le dépôt de départ utilisait `6000.3.21f1`. Ouvrir `Assets/_Project/Scenes/Game.unity`, choisir **CanonPOW → Installer la partie dans la scène ouverte**, enregistrer puis cliquer sur **Play**. Si l’installation existe déjà, le menu le signale.
4. Dans `Game`, ouvrir **F8 → Entrer en capture → Casse de la fronde**. Sur Android, poser trois doigts ensemble. Depuis la scène de Nathan, le bouton **Ouvrir la capture dans Game** donne accès au même panneau. Le menu **CanonPOW → Tests → Vérifier les règles de la jam** contrôle les fiches hors Play.

L’archive contient l’extension et ses réglages. Pour le jury, Benji exporte le projet Unity complet, le build PC et l’APK à partir de la branche réunie.

<!--page-->

## Boucle, contrôles et game over

### §2 · Boucle de jeu

L’accueil mène à la visée. Une casse de fronde ou une explosion de canon mène directement de Visée à Résultat. Cet échec avant départ rapporte 0 pièce, 0 m et 0 mur. Si le boulet est parti, la distance, les pièces ramassées et les murs déjà cassés comptent normalement. Un arrêt à faible vitesse garde l’écran Résultat ordinaire. Le tir suivant recommence au premier mur.

### §3 · Contrôles des deux lanceurs

| Lanceur | Action sûre | Alerte puis sanction |
| --- | --- | --- |
| Fronde en bois | Presser dans la moitié basse, glisser pour viser, relâcher dans le vert. Avec les réglages livrés, le vert va de 76 % à moins de 88 %. | Dès 88 %, la jauge est rouge, la fronde tremble, le grincement monte et les deux brins s’allongent. Relâcher ici casse la fronde. Garder le doigt puis relâcher après le retour au vert donne un tir normal. |
| Canon | Maintenir pour charger. À 1,4 s, le multiplicateur maximal est disponible. Relâcher avant 2,4 s tire normalement, y compris pendant l’alerte. | L’alerte monte entre 1,6 et 2,4 s. Le tube gonfle, rougit et tremble, le sifflement monte. À 2,4 s, le maintien suffit à déclencher l’explosion. |
| Lance-pierre géant, baliste, catapulte | Conservent la jauge oscillante habituelle. | La fronde géante peut utiliser la règle du bois avec `giantSlingCanBreak`, désactivé par défaut. |

Réglages de départ de la fronde et du canon dans `CanonPowJam.asset`. Zone rouge de 12 %, amplitude de 0,07 m et alerte perceptible au moins 0,15 s, à confirmer par [Andy]. Seule la fronde en bois casse par défaut, à confirmer par [Andy et Nathan]. Fenêtres du canon de 1,4 / 1,6 / 2,4 s, à confirmer par [Andy].

### §5 · Économie et monétisation

Le mur ordinaire touché en roulant termine le tir. **RELANCER avec une pub** présente la fausse pub de 2 s et redonne une seule tentative dans cette partie. Le boulet repart du dernier mur cassé. Si le tir a échoué avant le premier mur, il repart du lanceur avec la vitesse de tir mémorisée. Cette extension de la relance est à confirmer par [Hugo et Benji]. **REJOUER** démarre une nouvelle partie. Une victoire contre le boss mène au résultat de victoire.

Le gain garde la formule entière du GDD, arrondie vers le bas, `(0,5 × distance + pièces ramassées) × multiplicateur Argent + 20 × murs cassés`. Après relance, seule la hausse de gain est créditée. Le bonus ×2 existant reste possible sur les gains admissibles. Le résultat propose uniquement des actions de jeu et des pubs simulées.

<!--page-->

## Murs et objectif à long terme

### §4 · Le Mur géant

Le Mur géant occupe une position réelle dans le couloir. La partie rejoint le boss quand la suite des murs ordinaires atteindrait ou dépasserait sa distance. La même collision lit la vitesse du boulet en m/s en jeu normal, avec **Passer**, et pendant une prise. Chaque arrivée, en vol ou en roulant, enlève des PV. Si le mur tient, le tir s’arrête. Si le dernier PV tombe, le mur s’effondre et la partie se termine par une victoire.

| Fiche `MurGeant.asset` | Valeur initiale | Justification pour le playtest |
| --- | --- | --- |
| Distance | 1 000 m depuis le lanceur | Avec la suite du GDD commençant à 50 m et multipliée par 1,4, le dixième mur se trouverait vers 1 033 m. Le boss à 1 000 m donne une cible lisible, proche de cette étape. |
| Résistance et dégâts | 3 000 PV. Dégâts = vitesse d’impact × 5, arrondis à l’entier le plus proche. Minimum 1 pour un impact positif, maximum les PV restants. | À 70 m/s, un impact inflige 350. Huit de ces impacts laissent 200 PV, le neuvième détruit le mur. À 120 m/s, cinq impacts suffisent. Il s’agit d’impacts réussis, pas d’un nombre garanti de parties. |
| Fissures | 75 %, 50 %, 25 %, seuils inclus | À 2 250, 1 500 et 750 PV, changer de silhouette ou de maillage. Un seul gros impact peut franchir plusieurs seuils. |
| Présentation | Épaisseur 3 m, hauteur 14 m, largeur 14 m. 24 débris. | Le mur domine le couloir. Une barre suit son sommet. Les dégâts réels montent au point d’impact. À 60 PV, le coup final affiche −60. |

**À valider en jeu** — distance, 3 000 PV et coefficient 5, à confirmer par [Sami et Nathan]. Le prefab de départ propose aussi une progression plus serrée que le ×1,4 du GDD. Le rang exact du boss dépend des distances choisies. Le menu existant pour appliquer les distances du GDD reste disponible pour un essai comparatif.

**Sauvegarde.** Les PV sont écrits à chaque impact positif. Une ancienne sauvegarde démarre avec le boss à pleine santé et conserve ses achats. Une destruction est enregistrée à 0 PV. Les parties suivantes retrouvent la progression ordinaire au-delà de 1 000 m. Ce choix de fin et la possibilité d’endommager le boss en roulant sont à confirmer par [Sami et Benji].

### Règles du GDD qui évoluent

1. La zone de puissance la plus haute devient dangereuse sur la fronde en bois. Le canon remplace l’oscillation par un maintien chronométré.
2. Un tir peut désormais se terminer avant le vol. La non-casse ordinaire en roulant gagne un écran et un retour audiovisuel dédiés.
3. Le boss est une exception au mur ordinaire qui casse en vol au premier contact. Tant qu’il est debout, il devient le dernier obstacle. Les dégâts persistent entre les parties.
4. L’achat existant du canon gagne une mise en scène. Le menu de tournage ajoute des réglages temporaires pour rejouer les nouveaux plans.

<!--page-->

## Interface, effets et tournage

### §6 · Ce que voit et entend le joueur

| Déclencheur | À l’impact | Écran et action |
| --- | --- | --- |
| Fronde relâchée dans le rouge | Claquement, 8 éclats réutilisés, boulet qui tombe juste devant, petite secousse, « CRAC ! ». | « TROP FORT ! », puis RELANCER ou REJOUER. |
| Canon maintenu jusqu’au maximum | Explosion cartoon, morceaux projetés, fumée arrondie et voile de suie léger, « BOUM ! ». | « OUPS. », puis RELANCER ou REJOUER. |
| Mur ordinaire touché en roulant | Petit « toc », ralenti à 28 % de la vitesse normale, désaturation à 85 %. | « MUR INTACT… », score et distance restante jusqu’au mur placé après celui de l’impact. Pour des murs à 50 et 70 m, compter environ 20 m. |
| Mur géant | Son d’impact, perte de PV, nombre flottant et fissures. Le coup final projette les 24 morceaux et maintient le cadre. | « LE MUR GÉANT TIENT… » ou « MUR GÉANT DÉTRUIT ! ». Le résultat attend 1,3 s pour laisser lire l’impact. |
| Premier achat réussi du canon | La fronde s’envole avec ses confettis, le canon descend, rebondit dans la poussière, le flash et le jingle ponctuent l’arrivée. | « CANON DÉBLOQUÉ ! » pendant environ 2 s, puis retour à la boutique. Le canon exige toujours 15 000 pièces et le mur 8 atteint. |

La lecture du « mur suivant » et la durée des effets sont à confirmer par [Hugo et Andy]. Les pertes ordinaires laissent 0,65 s aux effets avant l’affichage. La boutique masque temporairement son interface pendant la transition.

### §7 · Refaire un plan de la pub

**F8** sur PC ou **trois doigts ensemble** sur Android ouvre et ferme le panneau. Choisir **Entrer en capture**. Les achats, les PV, le solde et les records sont alors temporaires. **Fermer le panneau** garde la prise active. **Quitter la capture** rend la progression réelle.

| Plan | Commande de tournage |
| --- | --- |
| Ouverture sur le boss | Choisir les PV puis **Travelling vers le Mur géant**. La caméra parcourt la piste en 3 s et tient le dernier cadre. **Caméra libre** utilise les flèches et le glissé souris, ou le doigt pour avancer et tourner. |
| Échec ou game over | **Casse de la fronde**, **Explosion du canon** ou **Mur intact** fait jouer l’action jusqu’au résultat. **Viser librement** rend la main. |
| Déblocage | **Préparer le premier achat du canon** ouvre la boutique avec le mur et le prix requis. Acheter pour filmer le vrai déblocage. **Transition fronde → canon** permet de rejouer seulement l’animation. |
| Dernier coup | Placer le curseur à **2 %**, puis **Impact sur le Mur géant**. À 3 000 PV, il reste 60 PV. À 0 %, le mur est déjà détruit. |
| Cadre propre | La visée conserve la jauge. Boutique, game over, RELANCER et effets d’impact restent filmables. Le portefeuille de vol, le record et les commandes opérateur disparaissent après fermeture. |

Livrable vidéo du GDD, MP4 vertical 1 080 × 1 920, maximum 30 s, accroche dès les trois premières secondes. Puissance de tournage 130 m/s et Élan 85 %, travelling 3 s, à confirmer par [Nathan et Louis].

<!--page-->

## Équipe et gel du contenu

### §9 · Ce que chacun livre vendredi 20 novembre avant 23 h

| Personne | Livrable et critère d’acceptation |
| --- | --- |
| Andy | Vérifier les deux charges au doigt et à la souris, le retour du rouge au vert, la chute et la reprise après relance. Rendre jouables les fenêtres de timing. Valider la caméra et le ralenti avec Hugo. |
| Sami | Jouer jusqu’au boss hors capture. Vérifier l’impact en vol et en roulant, les trois fissures, la destruction unique, la suite du parcours et la cohérence de **Passer**. Communiquer les vitesses d’impact à Nathan. |
| Nathan | Valider achats et sauvegarde dans `Game`, tester les deux refus du canon et son premier déblocage. Vérifier que les prises ne débloquent rien pour le joueur. Régler PV/dégâts avec Sami et faire la démonstration du menu triche à Louis. |
| Hugo | Vérifier les textes, les boutons et leur lisibilité en portrait. Tester RELANCER une fois, le double clic pendant la fausse pub, REJOUER et la boutique après transition. Rapprocher les panneaux de `Dev` du HUD de la branche d’intégration avec Benji. |
| Louis | Livrer le Mur géant intact, ses trois états fissurés, sa destruction et le décor visible pendant le travelling. Cadrer une première prise sur le mur puis un dernier coup au format vertical avec Benji. |
| Killian | Livrer fronde, élastique, pièces, canon et ses morceaux. Fournir les six sons et écouter leurs transitions avec Andy. La prise en charge du mix final est à confirmer par [Killian]. |
| Benji | Réunir code, scène, UI et assets sur la branche d’équipe. Parcourir `Boot → Game` dans un build normal, sans menu triche. Vérifier un seul HUD, une seule caméra active et une seule attribution de gain. Lancer la recette PC et Android. |

**Ordre de coupe demandé.** Préserver en premier le game over, puis la fronde qui casse, puis le Mur géant réellement jouable, puis la transition, puis l’explosion du canon. Le tournage utilise les fonctionnalités qui passent la recette dans le build.

**Samedi 21 novembre, 8 h.** Gel du code. Benji centralise le build PC et l’APK essayés sur deux appareils selon le GDD. Louis et Benji terminent le montage. **À 12 h**, le jury doit pouvoir voir le film et jouer la même boucle.

### §6 · Commandes de modèles et de sons

| Responsable | Fichiers à livrer et branchement |
| --- | --- |
| Louis | Mur intact et 3 FBX fissurés aux mêmes dimensions, dans l’ordre intact / 75 / 50 / 25. Fournir aussi le mur prédécoupé, 24 morceaux proposés dans la fourchette 10 à 30 du GDD. Assigner les quatre prefabs et les variantes de fragments dans `MurGeant`. |
| Killian | Fronde intacte avec élastique séparé + 6 à 10 éclats de bois, canon intact + 8 éclats proposés. Importer comme prefabs, monter les variantes dans `CanonPowJam`. Huit morceaux par lanceur, à confirmer par [Killian]. |
| Killian | Six WAV mono PCM 16 bits / 48 kHz. Boucles propres pour grincement et sifflement. Sons courts pour claquement, explosion cartoon, toc et jingle. Les six sons de maquette livrés permettent déjà d’écouter les prises. |

FBX, 1 unité = 1 m, Y vers le haut, piste vers +X et face avant des murs vers −X. Pivot de l’objet assemblé en bas au centre. Origine identique sur les quatre états du boss. Matériau URP Simple Lit, texture dégradée partagée du GDD. Débris préchargés en pool. Les modèles livrés avec l’extension sont des formes de maquette générées dans Unity.


<!--page-->

## Branches et contrats de l’équipe

### État relu sur GitHub le 20 novembre

| Branches distantes | Conséquence pour ton travail |
| --- | --- |
| `integration/codex`, `5f3d732` | Base de la livraison. Elle réunit `RunUpgrades`, `RunProgress`, la boucle de vol, les murs, le HUD de partie et l’installateur de la scène. Les modifications de l’extension sont disponibles dans le patch local. |
| `Nathan-Système-amelioration`, `Game`, `camera`, `3D-Terrain`, `Killian`, `evolution` | Leurs sommets relus sont déjà dans l’historique de cette base. Les cinq fiches d’amélioration, les essais d’achat, les lanceurs, le terrain et les travaux d’intégration peuvent servir aux tests. |
| `Dev`, `eddfcde`, et `Hugo`, `5440832` | L’interface récente possède ses panneaux Menu, Shop et Result et son `UIStateSwitcher`. Hugo et Benji doivent raccorder les affichages au même résultat et tester les boutons dans la scène choisie. |
| `Sami`, `8f9cd7a` | `UpgradeApplier` sait déjà appliquer tes statistiques dans sa sandbox via `upgrade-test.json`. Les deux méthodes `SetUpgrades` sont compatibles dans la livraison. Quand `RunUpgrades` est assigné dans `Game`, les valeurs viennent de la boutique de partie. |
| `main` et `Andy`, `2a8953a` | Même commit au moment de la lecture. Utiliser le commit de la branche où se trouve la boucle complète pour essayer l’extension. |

L’erreur de lecture de `Mini Simple Characters Skeleton Demo/Readme.asset` venait du pack de démo. Le commit `ecb7688` l’a supprimé et cette correction figure dans la base retenue. Si l’erreur réapparaît dans une autre copie, comparer d’abord sa branche et son commit. [Branches du dépôt](https://github.com/LaHasba92/Game-Jam-2026/branches)

### §8 · Événements de jeu

| Événement | Producteur | Consommateur et effet |
| --- | --- | --- |
| `OnChargeWarning(kind, force)` | `LauncherController` pendant la charge et au retour au calme | `JamLauncherFx` règle le son, l’étirement, la couleur et le tremblement. Le HUD lit la même intensité sur le lanceur. |
| `OnLauncherBroken()`, `OnCannonExploded()` | `LauncherController`, une fois la charge sanctionnée | `JamLauncherFx` produit le bris et le texte, `JamCameraFx` la secousse. Le projectile clôt ensuite la partie. |
| `OnGameOver(RunResult)` | `GameManager` après avoir accepté la fin et changé d’état | `FeedbackManager` ralentit l’impact, `JamCameraFx` désature et joue le toc pour les murs. `RunHud` lit `LastRun` au passage en Résultat. |
| `OnLauncherUnlocked(slot)` | `RunUpgrades` après débit et sauvegarde d’un premier achat | `JamLauncherFx` vérifie l’indice du canon et déclenche la présentation. Le slot de boutique commence à 0, le canon vaut 4. |
| `OnCannonReveal()` | `JamLauncherFx` après achat, ou `JamCaptureMenu` lors d’une reprise | `JamLauncherFx` anime la transition, `RunHud` masque puis rend la boutique. Rejouer la présentation ne réalise aucun achat. |
| `OnBossWallDamaged(BossImpact)` | `BossProgress` lors de `WallController.ApplyHit`, après sauvegarde | `BossWallView` affiche dégâts, barre, fissures et effondrement. `BossImpact` contient dégâts effectifs, PV restants et point d’impact. |

`IWall`, `WallHit` et les événements précédents conservent leurs signatures. `WallHitResult` gagne `bossHit`, faux par défaut. `RunResult` gagne `reason` et `distanceToNextWall`. Un ancien producteur qui ne renseigne rien donne `Distance`. La valeur `ShotInfo.launcherIndex` du contrat existant commence toujours à 1.

<!--page-->

## Index des modifications

Les scripts complets, fichiers de réglages Unity, sons et métadonnées sont sous `Sources/` dans l’archive. Les chemins ci-dessous sont relatifs à `Assets/`. Le patch est également fourni pour examiner chaque différence.

### Scripts de l’équipe modifiés

| Fichier | Changement |
| --- | --- |
| `PowerPurchaseTest.cs` | Ajoute le bouton de passage de la sandbox de Nathan à la capture dans Game. |
| `Scripts/Launcher/LauncherController.cs` | Calcule charge et alerte selon le lanceur, traite le relâchement ou le dépassement et reproduit le geste en capture. Accepte l’entrée de la sandbox de Sami. |
| `Scripts/Launcher/ProjectileController.cs` | Chute du boulet, raisons de fin, distance au mur suivant, impacts du boss et relance avant le premier mur. Utilise la même simulation pour les prises et pour Passer. |
| `Scripts/Launcher/CameraDirector.cs` | Secousse, cadrage du Mur géant et positionnement au début des prises. |
| `Scripts/Launcher/GameplayPointer.cs` | Bloque le début d’un geste de jeu pendant la manipulation du panneau opérateur. |
| `Scripts/Wall/LevelDefinition.cs` | Décrit PV, dégâts, distance, modèle de chaque palier et débris du boss. Zéro PV définis sur une fiche désigne un mur ordinaire. |
| `Scripts/Wall/LevelManager.cs` | Insère le boss dans la succession des murs et reprend le parcours après sa destruction persistante. Fournit l’accès de capture. |
| `Scripts/Wall/WallController.cs` | Aiguille l’impact vers les règles ordinaires ou les PV du boss et produit le résultat. |
| `_Project/Scripts/Core/Contracts.cs` | Deux champs de résultat et indicateur de boss, tout appel existant conservé. |
| `_Project/Scripts/Core/GameEvents.cs` | Déclare les sept événements de l’extension et leurs méthodes d’émission. |
| `_Project/Scripts/Core/GameManager.cs` | Accepte un raté en Visée, contrôle la relance et distingue défaite et victoire. |
| `_Project/Scripts/Core/RunProgress.cs` | Lit et écrit le portefeuille, les murs atteints et le record via la sauvegarde commune. |
| `_Project/Scripts/Core/RunUpgrades.cs` | Émet le premier achat après succès, prépare un achat de tournage et fournit les statistiques temporaires. |
| `_Project/Scripts/Feedback/FeedbackManager.cs` | Coordonne le ralenti et sa restauration avec les mécanismes de pause existants. |
| `_Project/Scripts/UI/RunHud.cs` | Affiche les états de la jauge et du résultat, branche le panneau capture, masque les éléments de tournage et retarde le résultat pour laisser voir l’effet. |
| `_Project/Scripts/Editor/CanonPowSetup.cs` | Réutilise l’installation de Game dans la copie destinée au build. Charge des fiches de lanceur de secours si les fiches locales sont absentes. |

Le fichier `ProjectSettings/ProjectVersion.txt` cible Unity 6000.3.29f1, révision `96d2fe17529c`, pour cette copie. La base Git est en 6000.3.21f1. La version du build commun est à confirmer par [Benji]. [Version officielle de l’éditeur](https://unity.com/releases/editor/whats-new/6000.3.29f1)

<!--page-->

## Nouveaux fichiers et données

### Sous `Assets/_Project/Scripts/Jam/`

| Fichier | Rôle |
| --- | --- |
| `AimTuning.cs` | Fiche de réglages commune aux lanceurs, aux effets et à la capture. |
| `JamRules.cs` | Causes de fin, zone rouge, intensité de l’alerte du canon, formule de dégâts et seuils de fissures. |
| `JamSave.cs` | Conserve les clés PlayerPrefs existantes en jeu. La capture lit la sauvegarde et écrit uniquement en mémoire. |
| `BossProgress.cs` | Lit les PV, plafonne les dégâts, sauvegarde au contact et émet l’événement d’impact. |
| `BossWallView.cs` | Construit ou charge les quatre apparences, affiche la barre et les nombres au-dessus du mur, lance la destruction. |
| `JamLauncherFx.cs` | Anime et sonorise la charge, le bris et la transition. Restaure les couleurs et la position des modèles. |
| `JamCameraFx.cs` | Désaturation URP, secousse, travelling et caméra libre. |
| `JamBurst.cs` | Prépare les fragments et les nuages une fois et les réutilise pour les impacts suivants. |
| `JamGeometry.cs` | Construit les formes de maquette et applique les couleurs avec le matériau partagé. |
| `JamCaptureMenu.cs` | Rassemble les commandes de prise, la préparation d’un achat et les PV temporaires. |

### Éditeur, sons et fiches

| Chemin sous `Assets/_Project/` | Contenu |
| --- | --- |
| `Scripts/Editor/JamBuildScene.cs` | Prépare la copie de la scène Game au build et vérifie la présence de la boucle et du boss. |
| `Scripts/Editor/JamRuleChecks.cs` | Commande de vérification des réglages, des sons, des dégâts et de l’isolation de la capture dans l’éditeur. |
| `Resources/CanonPowJam.asset` | Valeurs de départ, sons et références à la fiche du boss et au matériau. |
| `Data/MurGeant.asset` et `Data/JamGreybox.mat` | La fiche du mur à 1 000 m et son matériau URP Simple Lit de maquette. |
| `Data/JamLanceurs/` | Cinq fiches de secours pour le build. `Fronde`, `LancePierre`, `Baliste`, `Catapulte` et `Canon` gardent les prix et conditions du GDD. |
| `Resources/JamAudio/` | Six sons mono livrés et assignés. `creak.wav`, `snap.wav`, `whistle.wav`, `boom.wav`, `tap.wav` et `unlock.wav`. |

Dans l’Inspector, choisir **CanonPowJam** pour la zone rouge, les temps du canon, les sons, les effets et le travelling. Choisir **MurGeant** pour la distance, les PV, les dégâts, les fissures, les meshes et la barre. Les réglages de maquette sont inclus afin de pouvoir commencer les tests avant la livraison des FBX.

<!--page-->

## Recette PC et Android

Checklist à cocher dans les applications construites. Sur PC, utiliser la fenêtre portrait 540 × 960 et la souris. Sur Android, tester en portrait au doigt sur deux appareils et noter le modèle, les FPS et le résultat. Cible GDD de 60 images/s sur un Android de milieu de gamme.

| Cas | Manipulation PC | Manipulation Android | Résultat attendu sur les deux |
| --- | --- | --- | --- |
| 1. Fronde | Tester 3 relâchements, vert, première image rouge, retour au vert. | Refaire au doigt et annuler un toucher. Filmer un essai rouge. | Deux tirs sûrs. Dans le rouge, alerte avant le claquement, 6 à 10 éclats, boulet qui tombe, CRAC puis TROP FORT. Annuler ne tire pas. |
| 2. Canon | Relâcher à 1,4 s, puis vers 2 s. Maintenir au-delà de 2,4 s. | Refaire en maintenant un seul doigt, puis interrompre un geste. | Les deux relâchements tirent. Le maintien complet gonfle et rougit le canon, fait monter le sifflement, puis donne fumée, voile sombre, BOUM et OUPS. |
| 3. Mur ordinaire | Toucher en vol puis en roulant. Comparer deux distances de mur connues. | Refaire en partie, puis utiliser **Mur intact** pour répéter. | En vol le mur casse. En roulant, toc, ralenti, image désaturée, MUR INTACT et bonne distance. La couleur et la vitesse reviennent à la reprise. |
| 4. Relance et argent | Au résultat, cliquer RELANCER deux fois rapidement. Retomber en échec, puis REJOUER. | Même essai au doigt, puis mettre en arrière-plan pendant la fausse pub. | Une pub validée et une relance par partie. Reprise au bon endroit, aucune pièce comptée deux fois, pas d’achat réel. REJOUER restitue un droit de relance. |
| 5. Achat du canon | Tester mur 7, mur 8 avec 14 999, puis mur 8 avec 15 000 pièces. Rééquiper. | Faire le vrai premier achat, rouvrir la boutique, quitter puis redémarrer. | Refus dans les deux premiers cas. Un débit et une transition d’environ 2 s au succès. Confettis, rebond, poussière, flash et titre. Rééquiper reste silencieux. |
| 6. Boss réel | Démarrer depuis Boot hors capture. Atteindre 1 000 m et comparer PV avant et après contact. | Même parcours dans l’APK, puis quitter complètement l’appli et la relancer. | PV visibles près du mur, dégâts cohérents avec la vitesse d’impact, progression sauvegardée. Une nouvelle partie reprend au premier mur avec le boss déjà abîmé. |
| 7. Fissures et victoire | En capture, préparer 76/75 %, 51/50 %, 26/25 %, puis 2 %. Refaire deux prises. | Même manipulation, finir le boss puis refaire un essai hors capture. | Trois changements aux seuils. À 2 %, un coup suffisant enlève 60 PV. Un seul effondrement et résultat de victoire. Le parcours normal retrouve les PV du joueur. |
| 8. Capture et interface | F8, travelling deux fois, caméra libre, boutique, chaque échec. Sortir. | Trois doigts, refaire chaque commande et vérifier la taille des boutons. | Les prises sont rejouables, la jauge et RELANCER restent lisibles. Solde, canon équipé, record et PV réels identiques avant et après. |
| 9. Régression et budget | Tester les autres lanceurs, Passer, la pause, un nouvel achat et plusieurs reprises. | Tester après 10 explosions successives, avec le Profiler, puis mettre en veille et revenir. | Lanceurs ordinaires utilisables, gains cohérents, aucun double HUD, débris réutilisés, pas de dérive du temps de jeu ni d’erreur Console. |

**Vérifications effectuées hors Unity.** 52 contrôles C# exécutés sur les règles, les contrats, les achats, le parcours, la relance et la sauvegarde avec des doublures de l’environnement Unity. La syntaxe des 28 fichiers C# de l’extension a également été analysée. Les contrôles d’image, de son, de toucher, de performances et de build PC/APK ci-dessus restent à exécuter dans Unity et sur appareil.
