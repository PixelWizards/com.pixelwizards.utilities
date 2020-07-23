# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.2.0-preview.5]
- added internal cache to the multi scene swap helper so the timeline track doesn't hammer us with load / unload calls too much
- wrapped editornote so it works in builds

## [0.2.0-preview.4]
- added non async version of the timeline track scene loader - options to load async or not on the multi-scene timeline track

## [0.2.0-preview.3]
- update to the scene swap timeline track - now properly 'tetrahedralizes' lightprobe data for additive GI baked light scenarios

## [0.2.0-preview.2]
- added multi-scene swap timeline track
- properly wrap editor stuff in defines so it works at runtime
- new 'Multi-Scene Swap Helper' - add one to a scene to bind Timeline to

## [0.2.0-preview.1]
- did a pass to rework the shortcuts to use the new shortcut manager, moved the various tools to more appropriate locations (not just stuck under a 'tools' menu)
- refactored gameobject replacement from a scriptable wizard to a dedicated window that can be persistent and docked

## [0.1.0-preview.14]
- added an auto increment number for object replacement utility

## [0.1.0-preview.13]
- moved 'load all scenes' button for multi-scene loader outside of the collapsed list of individual config loaders

## [0.1.0-preview.12]
- UI refactor for multi-scene - foldouts for configs / loader buttons, added explicit 'save' button 

## [0.1.0-preview.11]
- refactor of multi-scene saving so we aren't hammering the assetdb so badly

## [0.1.0-preview.10]
- added multi-scene loading system (scriptable object + runtime API)

## [0.1.0-preview.8]
- added texture pack utility

## [0.1.0-preview.6]
- removed uv tools 

## [0.1.0-preview.5] - 2020-01-16
- merged PR from Favo, version bump

## [0.1.0-preview.4] - 2020-01-16
- added in grouping, some additional shortcuts (need to refactor to use the new shortcut system still)
- added material / object replacement utilities

## [0.1.0-preview.3] - 2020-01-13
- version bump for openupm support

## [0.1.0-preview.1] - 2019-07-27
- initial project setup, structure

