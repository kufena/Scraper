# Site Scraper

This is an attempt to write a site scraper.

**Note: you may be breaking copyright rules if you use this scraper, so
please get permission before you use it.**

You give the scraper two arguments:

    **site to scrape**  **directory to write site**

The root site will need to have a file name, like index.html, otherwise
it won't know where to write the initial file.

It then looks for links on the same site, downloads files and saves them
in the directory given in the second argument.
Any html files are then searched for link, a and img tags, to find
other links to download.

