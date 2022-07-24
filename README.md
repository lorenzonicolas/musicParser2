# MusicParser

## Como ejecutar
 Sin ningun parametro: corre el lifecycle base.

 Lifecycle: fixea los discos nuevos. Se basa en la config `folderToProcess`. Lee todo lo que está ahí y lo intenta fixear por completo. Si lo logra, lo mueve a la carpeta `done_dir`. Sino, irá a alguna carpeta de error. Puede que vaya a casi listo, si solo hace falta confirmacion de tags (asegurar el género o país, por ejemplo).

 Tagfix: este va a la carpeta definida en `tag_fix_dir`. Se fija qué falta fixear de tags. Es con interacción, hay que ir uno por uno a ver qué falló.

 CountryFix: Sirve para fixear la metadata. Hay muchas bandas sin país, de cuando no tenía andando la metal-archives api. Correr para fixearlos, es automático no pide entry manual por el momento.

 Resync: vuelve a armar el archivo de backup y metadata. ATENCION. Configurar bien `folderToProcess` antes de correr este, ya que puede pisar todo el archivo backup.


## Errores
- que no updatee el backupfile en cada nuevo disco detectado... que lo haga una sola vez

- Que no descargue el archivo de google en el startup.

## Ideas de mejoras
- agregar unit tests para poder probar más fácil los casos raros en vez de re-ejecutar todo como un boludo