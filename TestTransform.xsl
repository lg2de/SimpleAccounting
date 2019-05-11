<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	
	<xsl:variable name="yposition" select="0"/>

	<xsl:template match="/">
		<xEport>
			<xsl:for-each select="*">
				<xsl:apply-templates />
			</xsl:for-each>
		</xEport>
	</xsl:template>

	<!-- TABLE REPLACEMENTS -->

	<!-- table base infos -->
	<xsl:template match="table">
		<xsl:variable name="height">
			<xsl:value-of select="@lineheight*count(tr)"/>
		</xsl:variable>

		<xsl:if test="@topline">
			<xsl:element name="line">
				<xsl:attribute name="x2">
					<xsl:value-of select="@width"/>
				</xsl:attribute>
			</xsl:element>
		</xsl:if>

		<xsl:apply-templates select="columns"/>

		<xsl:if test="@headerline">
			<xsl:element name="line">
				<xsl:attribute name="x2">
					<xsl:value-of select="@width"/>
				</xsl:attribute>
			</xsl:element>
		</xsl:if>

		<xsl:apply-templates select="data"/>

		<xsl:if test="@bottomline">
			<xsl:element name="line">
				<xsl:attribute name="x2">
					<xsl:value-of select="@width"/>
				</xsl:attribute>
			</xsl:element>
		</xsl:if>
	</xsl:template>

	<!-- table header -->
	<xsl:template match="table/columns">
		<xsl:apply-templates select="column"/>

		<xsl:element name="move">
			<xsl:attribute name="rely">
				<xsl:value-of select="../@lineheight"/>
			</xsl:attribute>
		</xsl:element>
		<xsl:variable name="yposition" select="$yposition+../@lineheight"/>
	</xsl:template>

	<xsl:template match="table/columns/column">
		<xsl:element name="text">
			<xsl:attribute name="x">
				<xsl:value-of select="@x"/>
			</xsl:attribute>
			<xsl:value-of select="text()"/>
		</xsl:element>
	</xsl:template>

	<!-- table data -->
	<xsl:template match="table/data">
		<xsl:apply-templates select="tr"/>
	</xsl:template>

	<!-- table rows -->
	<xsl:template match="table/data/tr">
		<xsl:message>table row</xsl:message>
		<xsl:if test="@leftline">
			<xsl:element name="line">
				<xsl:attribute name="y2">
					<xsl:value-of select="../../@lineheight"/>
				</xsl:attribute>
			</xsl:element>
		</xsl:if>
		<xsl:if test="@rightline">
			<xsl:element name="line">
				<xsl:attribute name="x1">
					<xsl:value-of select="../../@width"/>
				</xsl:attribute>
				<xsl:attribute name="y2">
					<xsl:value-of select="../../@lineheight"/>
				</xsl:attribute>
			</xsl:element>
		</xsl:if>

		<xsl:apply-templates select="td"/>

		<xsl:element name="move">
			<xsl:attribute name="rely">
				<xsl:value-of select="../../@lineheight"/>
			</xsl:attribute>
		</xsl:element>
		<xsl:variable name="yposition" select="$yposition+../../@lineheight"/>
		<xsl:if test="$yposition>=100">
			<xsl:message>new page</xsl:message>
			<!-- xsl:variable name="yposition" select="0"/ -->
			<xsl:apply-templates select="/xeport_high/page_footer" mode="page"/>
			<newpage/>
			<xsl:apply-templates select="/xeport_high/page_header" mode="page"/>
			<xsl:apply-templates select="../../columns"/>
		</xsl:if>

		<xsl:if test="@line">
			<xsl:element name="line">
				<xsl:attribute name="x2">
					<xsl:value-of select="../../@width"/>
				</xsl:attribute>
			</xsl:element>
		</xsl:if>
	</xsl:template>

	<!-- table cells -->
	<xsl:template match="table/data/tr/td">
		<xsl:variable name="trnum">
			<xsl:value-of select="position()"/>
		</xsl:variable>
		<xsl:element name="text">
			<xsl:attribute name="x">
				<xsl:value-of select="../../../columns/column[@id and @id=$trnum]/@x"/>
			</xsl:attribute>
			<xsl:value-of select="text()"/>
		</xsl:element>
	</xsl:template>

	<xsl:template match="line|text|newpage | @* | node()">
		<xsl:copy>
			<xsl:apply-templates select="@* | node()"/>
		</xsl:copy>
	</xsl:template>
	
</xsl:stylesheet>