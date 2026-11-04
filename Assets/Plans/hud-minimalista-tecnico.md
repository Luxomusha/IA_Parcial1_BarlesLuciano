# Project Overview
- Game Title: IA_Parcial1_BarlesLuciano (nombre actual del proyecto; no se cambiará).
- High-Level Concept: Simulación de IA con un cazador que busca y recolecta, evita o enfrenta lobos y alcanza una salida. Este trabajo modifica únicamente la presentación del feedback existente.
- Players: Observación de agentes autónomos; no se agregarán controles de personaje ni multijugador.
- Inspiration / Reference Games: No especificadas; referencia visual: panel de monitoreo técnico minimalista.
- Tone / Art Direction: Insignias oscuras, símbolos blancos de trazo uniforme, acentos de color discretos y transiciones breves.
- Target Platform: StandaloneWindows64, sin cambiar configuración.
- Screen Orientation / Resolution: Resolución predeterminada actual 1024x768; HUD adaptable también a 1280x720 y 1920x1080. No cambiar resolución del proyecto.
- Render Pipeline: URP 17.3.0, Unity 6000.3.17f1; conservar iluminación y pipeline.

# Game Mechanics
## Core Gameplay Loop
Conservar íntegramente las FSM, recolección, detección, persecución, aullidos, búsqueda, huida y navegación. El HUD traduce estados y eventos públicos existentes a símbolos: reduce ruido visual y permite comprender rápidamente a cada agente. La leyenda y la vista técnica conservan la claridad necesaria para presentar el parcial.

## Controls and Input Methods
Backend verificado: New Input System, paquete 1.19.0. Agregar únicamente botones de interfaz «Leyenda» y «Debug», manejados por un EventSystem con InputSystemUIInputModule. No usar UnityEngine.Input ni modificar controles existentes. Vista normal por defecto; leyenda inicialmente oculta y disponible con un clic.

# UI
Wireframe conceptual (los nombres entre corchetes representan imágenes, no texto final):
```
┌ CAZADOR ──────────────────┐
│ [estado]   OBJETIVO  1/3  │
│            ■ □ □         │
├ MANADA ───────────────────┤
│ L01   [estado]  [evento]  │
│ L02   [estado]  [evento]  │
│ L03   [estado]  [evento]  │
│ L04   [estado]  [evento]  │
│ [Leyenda]       [Debug]   │
└──────────────────────────┘
        escenario: [insignia] sobre cada agente
```
- Panel anclado arriba a la izquierda, margen de 16 unidades, ancho aproximado 260–280. Lista con desplazamiento si la manada crece.
- Fondo #151A22 con opacidad aproximada 85%, texto claro, borde sutil y esquinas suavizadas. Símbolos blancos con acento de estado; no comunicar información solamente mediante color.
- Canvas Screen Space Overlay y CanvasScaler Scale With Screen Size con referencia 1920x1080. Validar tamaños legibles a 1024x768 y limitar el panel a la pantalla.
- Insignias flotantes de aproximadamente 32–40 unidades. Sin nombres FSM en vista normal. Actualizar su posición en LateUpdate usando la cámara asignada; ocultar detrás de cámara o fuera del viewport. Mantener la política actual respecto a obstáculos: no agregar oclusión física.
- Estados del cazador: Patrol/huellas, Search/lupa, Attack/espadas, Gather/bolsa, Flee/corredor, Exit/puerta con flecha, Dead/calavera. Lobos: Patrol/huellas, Chase/mira, Investigate/interrogación, Search/lupa.
- Eventos separados del estado: aullido/ondas, alerta/exclamación. Ruta con nodos para A* solamente en debug. Animación breve al cambio de estado, sin parpadeos permanentes.
- Progreso: segmentos según RequiredGathers, con contador numérico; para cantidades elevadas evitar desbordamiento y conservar el contador.
- Debug: nombres exactos FSM, A*, emisor de alerta, lobo detectado y zona segura elegida. Marcas de última posición conocida mediante símbolo de pista y pequeño identificador de lobo, visibles únicamente en debug.
- Leyenda compacta con símbolo y explicación en español, sin cubrir la pantalla completa.

# Key Asset & Context
Estado verificado: Assets/Scenes/Parcial2.unity contiene un SimulationHUD con Hunter y Main Camera asignados; panel, etiquetas y últimas posiciones activados. Hay un cazador, cuatro lobos, ningún Canvas ni EventSystem activo y no se encontraron sprites en Assets. uGUI 2.0.0 ya está instalado.

Assets a crear:
- Assets/Prefabs/UI/SimulationHUD.prefab: Canvas, panel, filas, insignias y leyenda.
- Assets/Scripts/Feedback/SimulationHUDView.cs: controlador de presentación; solo lee APIs existentes, actualiza cuando cambia la información y reutiliza filas/insignias.
- Assets/Scripts/Feedback/HUDStateVisuals.cs: correspondencia de estados, nombres de leyenda, colores y sprites.
- Assets/UI/HUD/Icons/: PNG transparentes de 128x128 para estados y eventos; arte simple determinista, sin dependencia de generación externa, emojis ni paquetes SVG. Importar como Sprite, filtro bilinear, sin mipmaps. Fondo reutilizable de esquinas suaves.
- Assets/UI/HUD/HUDStateVisuals.asset: configuración visual compartida si se implementa como ScriptableObject.

Assets a modificar:
- Assets/Scenes/Parcial2.unity: incorporar el prefab, asignar Hunter/cámara y configurar EventSystem.
- No modificar SimulationHUD.cs: conservarlo como respaldo, pero desactivar su componente en esta escena para evitar HUD duplicado.

Contexto de solo lectura:
- Assets/Scripts/Feedback/SimulationHUD.cs: HUD actual OnGUI.
- Assets/Scripts/Hunter/Hunter.cs: HunterState, CurrentState, GatherCount, RequiredGathers, DetectedWolf, ChosenSafeZone, IsDead.
- Assets/Scripts/Wolf/Wolf.cs: WolfState, CurrentState, Pack, IsHowling, JustReceivedAlert, LastAlertSender, IsUsingPathfinding, HasLastKnownPosition, LastKnownPosition.
- Conservar scripts de FSM, agentes, prefabs de personajes y SampleScene. No instalar paquetes, alterar fuentes de paquetes ni añadir defines.

Alternativas evaluadas: retocar OnGUI sería más rápido, pero dificulta la edición visual y los layouts adaptables; Canvas/uGUI permite prefabs editables y una separación clara entre IA y presentación. Se adopta Canvas conforme al diseño aceptado.

# Implementation Steps
1. Revalidar referencias y dependencias/dependientes de Parcial2.unity y SimulationHUD.cs antes de editar; consultar el grafo de dependencias cuando esté disponible y comprobar referencias de escenas/prefabs con AssetDatabase. Verificar APIs públicas completas y fuente de texto disponible. Dependencias: ninguna.
2. Crear símbolos uniformes y configuración de estados en Icons/, HUDStateVisuals.cs y el asset visual. Utilizar fuente legible disponible; no descargar fuentes o asumir que existe una monoespaciada. Dependencias: paso 1.
3. Construir SimulationHUD.prefab con CanvasScaler, LayoutGroups, panel, filas reutilizables, botones y leyenda; separar panel e insignias en subcanvases para limitar reconstrucciones. Usar texto uGUI estándar con fuente asignada, sin nuevos recursos TMP. Dependencias: paso 2.
4. Implementar SimulationHUDView.cs: leer estados sin intervenir en la IA, mantener identificadores L01–L04 estables por instancia, seguir altas/bajas de Wolf.Pack, actualizar progreso, posicionar y reutilizar insignias y marcadores. Los pulsos se disparan al detectar cambios, no se reinician cada frame. Dependencias: pasos 1–3.
5. Incorporar el prefab en Parcial2.unity, asignar Hunter/Main Camera, desactivar solamente el componente SimulationHUD antiguo y agregar un único EventSystem con InputSystemUIInputModule si continúa ausente. Conectar botones Leyenda/Debug sin nuevos atajos. Dependencias: pasos 3–4.
6. Validar visualmente la Game view, ajustar tamaño/contraste y verificar una ejecución Windows con la escena correcta. No cambiar la primera escena ni la lista de build existente sin autorización; si Parcial2 no está incluida, usar un build de validación específico. Dependencias: paso 5.

# Verification & Testing
- Compilación sin errores y sin dependencias nuevas; consola sin excepciones al entrar/salir de Play Mode.
- Comprobar mapeo completo de los 7 estados Hunter y los 4 estados Wolf, con símbolos distintos para Search/Investigate. Estados no alcanzables en Parcial2 se verifican mediante configuración o vista de prueba, sin alterar la FSM real.
- Comparar información del HUD nuevo con APIs reales: progreso, estado, detección, zona elegida, aullido, alerta y A*.
- Una sola presentación de HUD; modo normal sin nombres de estados, debug con información técnica completa, botones operativos con New Input System.
- Cazador muerto: tarjeta muestra Dead cuando la referencia es válida, insignia se oculta; respawn recupera insignia y progreso reales.
- Sin Hunter, sin cámara y sin lobos: degradación controlada, sin NullReferenceException. Lobos destruidos/agregados: retirar o reutilizar vistas sin IDs cambiantes.
- Ocultar insignias detrás de cámara/fuera del viewport; verificar márgenes y posiciones con CanvasScaler en las tres resoluciones previstas.
- Probar alerta y aullido simultáneos sin sustituir el estado, retirada de marcadores al perder HasLastKnownPosition y ausencia de animaciones reiniciadas continuamente.
- Verificar que movimientos, rutas, temporizadores, lógica de combate y recolección no cambian. Inspección de diff limitada a UI, escena y nuevos recursos.
- Reentrada a Play Mode y recarga de escena sin suscripciones duplicadas ni objetos persistentes sobrantes. Build Windows de validación si el módulo está disponible; informar cualquier limitación real.

Este documento es un plan; no autoriza cambios hasta recibir aprobación formal.