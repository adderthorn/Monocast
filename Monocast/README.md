# Monocast Changelog #

## 1.2.28 ##

* Fixed bug where importing OPML files may fail if text/title is not set
* Added ability to import *.xml files as OPML
* Added ability to export podcasts as OPML file
* Fixed bug where podcasts with empty link elements for episodes would cause Monocast to crash.
* Fixed a bug where opening while offline can cause a crash
* Fixed a bug where attempting to replay an episode would not work

## 1.2.20 ##

4/18/2018 9:00:14 AM 

* Fixed a bug where downloads would be saved without a file extension
* Updated downloads view to take the user back to the last page after downloads complete
* Updated sync to no longer take a user away from the current page when sync is complete
* Updated Podcast view to support published date
* Made some minor adjustments to podcast view sizing
* Added tool tips to episode items to see whole title before clicking
* Added Acrylic theme to nav bar for supported versions of Windows 10

## v1.2.16 ##

3/10/2018 9:03:18 AM 

* Added ability to play/pause by tapping artwork in player view
* OPML import now shows which feeds it's parsing as it goes
* Adding a feed *without* the http/s prefix will now work correctly
* Added details button to podcast view

## v1.1.10.0

* Fixed a bug where launching when clicking a pcast:// link would cause the application to crash

## v1.1.9.0

* Fixed a bug where podcast duration was not being properly calculated for some feeds
* Fixed a bug where attempting to sync while offline would cause Monocast to crash
* Added import for OPMLs


## v1.1.5.0

* Initial release
* Support for multiple subscriptions
* Playback support with variable speed and skip ahead/skip back
* Supports high-resolution artwork
* Supports streaming or downloaded play
* Allows custom folder for saving to a particular locations
* Allows ad-hoc episode downloads
* Supports auto synchronization of feeds