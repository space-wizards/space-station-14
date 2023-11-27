<?xml version="1.0" encoding="UTF-8"?>
<html xsl:version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:ss14="https://spacestation14.com/changelog_rss">
<head>
  <style>
    <![CDATA[
      body {
        font-family: Arial;
        font-size: 1.6em;
        background-color: rgb(32, 32, 48);
        max-width: 1024px;
        margin: 0 auto;
      }
      .title {
        font-size: 1.2em;
        color: rgb(155, 34, 54);
        font-weight: bold;
        padding: 4px;
      }
      .author {
        font-size: 1.2em;
      }
      .description {
        margin-left: 20px;
        margin-bottom: 1em;
        font-size: 10pt;
        color: rgb(199, 199, 199);
      }
      a {
        text-decoration: none;
        color: white;
      }
      .changes li {
        list-style-type: none;
        padding: 1px;
      }
      li::before {
        margin-right: 6px;
      }
      li.Tweak::before {
        content: '🔧';
      }
      li.Fix::before {
        content: '🐛';
      }
      li.Add::before {
        content: '➕';
      }
      li.Remove::before {
        content: '➖';
      }
    ]]>
    </style>
</head>
<body>

  <xsl:for-each select="rss/channel/item">
    <div class='title'>
      <xsl:copy-of select="pubDate"/>
    </div>
    <div class='description'>
    <xsl:for-each select="*[local-name()='entry']">
      <div class='author'>
        <a target='_blank' href='https://github.com/space-wizards/space-station-14/commits'>
          <xsl:attribute name="data-author">
            <xsl:value-of select="*[local-name()='author']" />
          </xsl:attribute>
          <xsl:value-of select="*[local-name()='author']"/>
        </a> updated
      </div>
      <div class='changes'>
        <ul>
        <xsl:for-each select="*[local-name()='change']">
          <li>
            <xsl:attribute name="class">
              <xsl:value-of select="@*" />
            </xsl:attribute>
            <xsl:copy-of select="node()" />
          </li>
        </xsl:for-each>
        </ul>
      </div>
    </xsl:for-each>
    </div>
  </xsl:for-each>

  <script>
    <![CDATA[
      const authors = document.querySelectorAll('[data-author]');
      authors.forEach(author => {
        const authorName = author.getAttribute('data-author');
        author.setAttribute('href', `https://github.com/space-wizards/space-station-14/commits?author=${authorName}`);
      });
    ]]>
  </script>
</body>
</html>
