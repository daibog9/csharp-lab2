<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

  <xsl:output method="html" encoding="utf-8" indent="yes" doctype-system="about:legacy-compat"/>

  <xsl:key name="events-by-type" match="Event" use="@type"/>

  <xsl:template match="/">
    <html lang="uk">
      <head>
        <meta charset="utf-8"/>
        <title>Студентський парламент</title>
        <style>
          body { font-family: -apple-system, "Segoe UI", Roboto, Arial, sans-serif; margin: 32px; color: #1d1d1f; background: #fafafa; }
          h1 { margin: 0 0 4px; font-size: 26px; }
          .subtitle { color: #6e6e73; margin-bottom: 20px; }
          .info { background: #eef4ff; border: 1px solid #c9dafc; border-radius: 8px; padding: 10px 14px; margin-bottom: 18px; font-size: 14px; }
          .info b { display: inline-block; min-width: 150px; }
          .summary { display: flex; gap: 12px; flex-wrap: wrap; margin-bottom: 20px; }
          .chip { background: #fff; border: 1px solid #ddd; border-radius: 16px; padding: 4px 12px; font-size: 13px; }
          table { border-collapse: collapse; width: 100%; background: #fff; font-size: 13px; }
          th, td { border: 1px solid #d2d2d7; padding: 6px 8px; vertical-align: top; text-align: left; }
          th { background: #2c3e75; color: #fff; position: sticky; top: 0; }
          tr.first td { border-top: 2px solid #8e9ac7; }
          td.num { text-align: right; color: #6e6e73; }
          .name { font-weight: 600; }
          .tag { display: inline-block; background: #f0f0f5; border-radius: 4px; padding: 1px 6px; margin: 1px 2px 1px 0; white-space: nowrap; }
          .type { font-weight: 600; color: #2c3e75; }
          .muted { color: #a1a1a6; }
          .empty { padding: 24px; text-align: center; color: #6e6e73; background: #fff; border: 1px dashed #ccc; }
        </style>
      </head>
      <body>
        <xsl:apply-templates select="StudentParliament"/>
      </body>
    </html>
  </xsl:template>

  <xsl:template match="StudentParliament">
    <h1>Студентський парламент</h1>
    <div class="subtitle">
      <xsl:value-of select="@university"/>
      <xsl:if test="@academicYear">
        <xsl:text> · навчальний рік </xsl:text>
        <xsl:value-of select="@academicYear"/>
      </xsl:if>
    </div>

    <xsl:if test="@strategy">
      <div class="info">
        <div><b>Спосіб аналізу</b><xsl:value-of select="@strategy"/></div>
        <div><b>Параметри пошуку</b><xsl:value-of select="@filter"/></div>
        <div><b>Сформовано</b><xsl:call-template name="format-datetime"><xsl:with-param name="value" select="@generated"/></xsl:call-template></div>
      </div>
    </xsl:if>

    <div class="summary">
      <span class="chip">Учасників: <b><xsl:value-of select="count(Member)"/></b></span>
      <span class="chip">Записів про заходи: <b><xsl:value-of select="count(Member/Event)"/></b></span>
      <xsl:for-each select="Member/Event[generate-id() = generate-id(key('events-by-type', @type)[1])]">
        <xsl:sort select="@type" lang="uk"/>
        <span class="chip">
          <xsl:value-of select="@type"/>
          <xsl:text>: </xsl:text>
          <xsl:value-of select="count(key('events-by-type', @type))"/>
        </span>
      </xsl:for-each>
    </div>

    <xsl:choose>
      <xsl:when test="Member">
        <table>
          <thead>
            <tr>
              <th>№</th>
              <th>П.І.П.</th>
              <th>Факультет (департамент, відділення)</th>
              <th>Кафедра</th>
              <th>Спеціальність</th>
              <th>Інші атрибути</th>
              <th>Вид заходу</th>
              <th>Захід</th>
              <th>Початок</th>
              <th>Завершення</th>
              <th>Місце</th>
            </tr>
          </thead>
          <tbody>
            <xsl:apply-templates select="Member"/>
          </tbody>
        </table>
      </xsl:when>
      <xsl:otherwise>
        <div class="empty">За заданими параметрами нічого не знайдено.</div>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="Member">
    <xsl:variable name="member" select="."/>
    <xsl:variable name="number" select="position()"/>
    <xsl:variable name="rows" select="count(Event)"/>

    <xsl:choose>
      <xsl:when test="$rows = 0">
        <tr class="first">
          <xsl:call-template name="member-cells">
            <xsl:with-param name="number" select="$number"/>
            <xsl:with-param name="rows" select="1"/>
          </xsl:call-template>
          <td colspan="5" class="muted">заходів немає</td>
        </tr>
      </xsl:when>
      <xsl:otherwise>
        <xsl:for-each select="Event">
          <tr>
            <xsl:if test="position() = 1">
              <xsl:attribute name="class">first</xsl:attribute>
              <xsl:for-each select="$member">
                <xsl:call-template name="member-cells">
                  <xsl:with-param name="number" select="$number"/>
                  <xsl:with-param name="rows" select="$rows"/>
                </xsl:call-template>
              </xsl:for-each>
            </xsl:if>
            <xsl:apply-templates select="." mode="cells"/>
          </tr>
        </xsl:for-each>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="member-cells">
    <xsl:param name="number"/>
    <xsl:param name="rows"/>
    <td class="num" rowspan="{$rows}"><xsl:value-of select="$number"/></td>
    <td class="name" rowspan="{$rows}">
      <xsl:value-of select="normalize-space(concat(FullName/LastName, ' ', FullName/FirstName, ' ', FullName/Patronymic))"/>
    </td>
    <td rowspan="{$rows}"><xsl:value-of select="@faculty"/></td>
    <td rowspan="{$rows}">
      <xsl:choose>
        <xsl:when test="@department"><xsl:value-of select="@department"/></xsl:when>
        <xsl:otherwise><span class="muted">—</span></xsl:otherwise>
      </xsl:choose>
    </td>
    <td rowspan="{$rows}"><xsl:value-of select="@specialty"/></td>
    <td rowspan="{$rows}">
      <xsl:apply-templates select="@*[not(name() = 'id' or name() = 'faculty' or name() = 'department' or name() = 'specialty')]" mode="extra"/>
      <div class="muted">атрибутів: <xsl:value-of select="count(@*)"/></div>
    </td>
  </xsl:template>

  <xsl:template match="Event" mode="cells">
    <td>
      <span class="type"><xsl:value-of select="@type"/></span>
      <xsl:if test="@format">
        <div class="muted"><xsl:value-of select="@format"/></div>
      </xsl:if>
    </td>
    <td><xsl:value-of select="Title"/></td>
    <td><xsl:call-template name="format-datetime"><xsl:with-param name="value" select="Start"/></xsl:call-template></td>
    <td><xsl:call-template name="format-datetime"><xsl:with-param name="value" select="End"/></xsl:call-template></td>
    <td>
      <xsl:choose>
        <xsl:when test="Location"><xsl:value-of select="Location"/></xsl:when>
        <xsl:otherwise><span class="muted">—</span></xsl:otherwise>
      </xsl:choose>
    </td>
  </xsl:template>

  <xsl:template match="@*" mode="extra">
    <span class="tag">
      <xsl:choose>
        <xsl:when test="name() = 'course'">Курс</xsl:when>
        <xsl:when test="name() = 'group'">Група</xsl:when>
        <xsl:when test="name() = 'position'">Посада</xsl:when>
        <xsl:when test="name() = 'email'">Email</xsl:when>
        <xsl:otherwise><xsl:value-of select="name()"/></xsl:otherwise>
      </xsl:choose>
      <xsl:text>: </xsl:text>
      <xsl:value-of select="."/>
    </span>
  </xsl:template>

  <xsl:template name="format-datetime">
    <xsl:param name="value"/>
    <xsl:value-of select="concat(substring($value, 9, 2), '.', substring($value, 6, 2), '.', substring($value, 1, 4), ' ', substring($value, 12, 5))"/>
  </xsl:template>

</xsl:stylesheet>
