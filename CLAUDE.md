# Clean Slate — installer tous ses logiciels d'un coup

## Le projet
Application de bureau Windows (un seul `.exe`) : un grand catalogue de logiciels rangés par famille, on coche, on clique,
tout s'installe. Pensée pour un PC neuf ou tout juste réinstallé, et pour quelqu'un qui n'y connaît rien.
Utilisateur : Tom (BTS SIO). Toutes les réponses et l'interface sont **en français**.

Projet frère de **Reboot Camp** (`d:\Dev\Reboot Camp`) : on lui reprend le thème sombre, la fenêtre, les styles et la
plomberie (winget, journal, fermeture propre). **Rien d'autre** : le but n'est pas le même, aucun onglet de maintenance ici.

## Les trois écrans (décisions de Tom du 22/11/2026)
1. **Catalogue** : tuiles à cocher, familles dans la barre latérale, recherche. Bouton « Suivant ».
2. **Où installer ?** : seulement les logiciels dont le dossier est réglable, avec le chemin à côté du nom ; un clic
   dessus ouvre le sélecteur de dossier. Le choix se fait logiciel par logiciel (plus « Même dossier pour tous »).
   Les logiciels qui imposent leur emplacement sont résumés en une phrase.
3. **Installation** : un logiciel après l'autre, état et explication en français sous chaque nom.

Règles sur l'emplacement :
- On ne propose le choix que là où il sera respecté : `CatalogLocations.cs` (écrit par `verifier-catalogue.ps1` d'après
  les fiches du dépôt microsoft/winget-pkgs) liste les installeurs qui acceptent `--location`.
- Les **composants système** (Visual C++, .NET, DirectX, Java…) vont toujours sur le disque de Windows : jamais de choix.
- Après l'installation on vérifie que le dossier demandé contient bien quelque chose ; sinon la ligne passe à l'orange.

## Ajouts du 22/11/2026 (validés par Tom)
- **Packs** (Gamer, Bureautique, Étudiant, Streaming, Développeur) : pastilles en haut de « Les indispensables », un clic
  coche tout le lot. Définis dans `Catalog.Packs` ; aucun navigateur dedans (choix trop personnel).
- **Sélection dans un fichier** : « Enregistrer la sélection » / « Ouvrir une sélection » (JSON, `SelectionFile.cs`). Un
  fichier glissé sur le `.exe` est ouvert au lancement. Le fichier peut venir de quelqu'un d'autre : un dossier n'est
  repris que s'il a la forme d'un vrai chemin, sans guillemet (il finit dans la commande winget).
- **Applis du Microsoft Store** : `S(nom, identifiant du Store, nom du paquet Windows, description)` dans le catalogue,
  installées par `winget … --source msstore`. « Déjà installé » = le dossier `%LOCALAPPDATA%\Packages\<paquet>` existe
  (`winget export` ne liste pas les applis du Store). Jamais de choix de dossier.
- **Payant / Compte / Essai** : `CatalogAccess.cs`, liste tenue à la main ; le mot s'affiche devant la description de la
  tuile. Tout ce qui n'y est pas est « Gratuit » (dit dans l'infobulle seulement, pour ne pas charger les tuiles).
- **Espace disque** : l'écran « Où installer ? » affiche la place libre du disque visé, en orange sous 10 Go. winget ne
  donne pas la taille des logiciels : on ne promet donc pas « il manque X Go ».
- **Fin d'installation** : bouton « Ouvrir » si l'installeur a créé un raccourci (menu Démarrer ou Bureau, repéré en
  comparant avant/après, `Shortcuts.cs`), lancé avec le compte normal ; « Copier le résumé ».
- **Dernières versions** (exigence de Tom) : jamais de `--version`, et `winget source update` avant chaque installation.

## Ajouts du 22/11/2026, soir (demandes de Tom)
- **« Déjà installé » vérifié deux fois** : par winget (`winget export`), puis sur le PC lui-même (`InstalledApps.cs` :
  registre « Uninstall » et dossiers de `%LOCALAPPDATA%\Packages`). winget ratait Brave, WinRAR, Spotify du Store…
  Le nom doit correspondre exactement (version mise de côté) ; les autres noms et les paquets Windows sont notés à la
  main dans `InstalledApps.OtherNames` / `Packages`. Jamais de comparaison approximative (Opera ≠ Opera GX).
- **Famille « Intelligence artificielle »** : ChatGPT, Claude, Gemini, Copilot, Perplexity, IA locales…
- Barre de recherche : le modèle `SearchBox` ne doit pas remettre le `Padding` en marge (WPF l'applique déjà), sinon
  le texte tapé est invisible.

- **Installer quand même** : une tuile « Déjà installé » reste cliquable (la détection peut se tromper) ; elle affiche
  alors « Sera installé quand même ». Le lien « N déjà installés », sous la recherche, montre ces tuiles.
- **Étapes de l'installation** : lancé par un programme, winget n'écrit pas sa barre d'avancement et télécharge hors de
  son dossier temporaire (essayé le 22/11/2026) : pas de pourcentage possible. On affiche « Téléchargement… » à la
  ligne qui contient l'adresse du fichier (`Winget.IsDownloadLine`), puis « Téléchargé. Installation… ».
- « Réessayer ce qui a échoué » relance `_notDone` (et non « ce qui n'est pas installé », faux depuis « installer quand même »).

## Logos (demande de Tom du 22/11/2026 : les vrais logos, pas des pastilles de couleur)
- Chaque logiciel affiche son vrai logo, embarqué dans le `.exe` (`Assets/Logos/<identifiant winget>.png`, 64 x 64) :
  rien n'est téléchargé pendant que l'appli tourne.
- `telecharger-logos.ps1` les récupère (base d'icônes d'UniGetUI, puis collection « dashboard-icons », puis icône du
  site de l'éditeur ; table `$alias` pour les cas particuliers). À relancer après avoir ajouté un logiciel, puis
  **regarder le logo obtenu** : les bases contiennent quelques images fausses.
- Sans logo, la tuile retombe sur une pastille de couleur avec l'initiale (3 cas : K-Lite, Inkscape, OpenAL).
- Les applis du Store ont un identifiant sans nom (« 9NKSQGP7F2NH ») : leur logo passe toujours par la table `$alias`.

## Façon de travailler
- Avant d'ajouter un logiciel au catalogue, **vérifier que son identifiant winget existe** (`winget show --id … --exact
  --source winget`), puis relancer `verifier-catalogue.ps1`. Rien d'inventé.
- « Soit ça marche, soit ce n'est pas proposé » : un logiciel que winget n'installe pas en silencieux ne va pas au catalogue.
- Compiler, lancer `dotnet test`, regarder l'écran avant de dire que c'est fini.
- Expliquer brièvement ce qui a été fait (Tom veut comprendre le code).

## Stack technique
- **C# / WPF / .NET 10**, MVVM léger avec CommunityToolkit.Mvvm (rien d'autre sans demander).
- `winget` en ligne de commande, toujours par `Services/Winget.cs`. Jamais `--allow-reboot`, jamais `--ignore-security-hash`.
- Manifeste `requireAdministrator` : une seule demande au lancement au lieu d'une par logiciel. Les installeurs qui
  refusent les droits d'administrateur (code `0x8A150056`, ex. Spotify) sont relancés avec le compte normal (`StandardUser`).
- Publication automatique : chaque `git push` sur `main` lance `.github/workflows/publication.yml` (tests, `.exe` autonome,
  Release `v1.0.N` sur `TomRolling/Clean-Slate`, N = numéro de l'exécution). L'appli lit la dernière Release au lancement
  et propose « Mettre à jour » (téléchargement, empreinte vérifiée, échange des fichiers, relance : `UpdateService`).
  `publier.ps1` ne fait plus que fabriquer le `.exe` en local. La `<Version>` du `.csproj` (1.0.0) ne sert qu'en développement.
- L'appli s'appelait InstalleTout jusqu'au 22/11/2026 ; le dossier de travail `d:\Dev\InstalleTout` a gardé ce nom.
- Icône : étincelles blanches sur dégradé bleu → violet (`Assets/app.ico`), même dessin dans la barre latérale.
- Pour lancer la version de développement sans fenêtre UAC : variable d'environnement `__COMPAT_LAYER=RunAsInvoker`.

## Règles « appli clean »
- Aucun démarrage automatique, aucun service, aucune icône de zone de notification.
- **Fermer la fenêtre = quitter complètement** : tâches annulées, processus enfants tués (`AppLifetime`).
- Rien ne s'installe sans clic. À l'ouverture, deux questions seulement : à winget « qu'est-ce qui est déjà installé ? »
  (pour griser les tuiles) et à GitHub « y a-t-il une version plus récente ? ».
- Pas de télémétrie, pas de pub. Tout en `async`, l'interface ne gèle jamais.
- **Jamais de redémarrage du PC sans accord** (`RestartService`).
- Journal dans `%LOCALAPPDATA%\CleanSlate\logs`.

## Design
- Thème sombre de Reboot Camp, épuré : peu de texte, une action principale évidente par écran.
- La fin d'une action se voit : bouton rond vert « TERMINÉ » (orange si une partie a échoué), titre « Terminé : … ».
- Un échec winget connu est expliqué en français sous le nom du logiciel (`Winget.Explain`).

## Où est quoi
- `Services/Catalog.cs` : le catalogue (une ligne par logiciel ; `P(…)` = aussi dans « Les indispensables »).
- `ViewModels/CatalogueViewModel.cs` : toute la logique des trois écrans.
- `Views/` : `CatalogueView`, `ReviewView`, `InstallView`. Style des tuiles : `AppTile` dans `Themes/Controls.xaml`.
- `Services/CatalogAccess.cs` (payant / compte), `SelectionFile.cs`, `Disks.cs`, `Shortcuts.cs`, `InstalledApps.cs`.
- `CleanSlate.Tests` : catalogue sans doublon, codes de winget, enchaînement Catalogue → Où installer, packs,
  fichier de sélection, applis du Store.
- Essais à l'écran : capturer la fenêtre avec `PrintWindow` sans la mettre au premier plan (Tom peut être en jeu).
