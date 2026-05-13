function reportApp() {
  return {
    loading: true,
    errorMessage: '',
    projectPath: '',
    generatedAt: '',
    categories: [],
    summary: null,
    analysisIndex: null,
    categoryArtifacts: [],
    allFindings: [],
    expandedInventoryRows: {},
    filters: {
      category: '',
      componentType: '',
      severity: '',
      risk: '',
      findingsConfidence: '',
      findingsFalsePositiveRisk: '',
      topFalsePositiveRisk: '',
      topEffort: '',
      actionPriority: '',
      actionRemediationRisk: '',
      actionEffort: '',
      actionConfidence: '',
      checkStatus: '',
      checkImportance: '',
      search: ''
    },

    async init() {
      try {
        const [analysisIndex, summary] = await Promise.all([
          this.readJson('../analysis/analysis-index.json'),
          this.readJson('../analysis/component-analysis-summary.json')
        ]);

        this.analysisIndex = analysisIndex;
        this.summary = summary;
        this.projectPath = summary.projectPath || analysisIndex.projectPath || '';
        this.generatedAt = summary.generatedAtUtc || analysisIndex.generatedAtUtc || '';

        this.categories = this.canonicalCategoryOrder().filter((category) => {
          const selected = analysisIndex.selectedCategories || [];
          return selected.includes(category);
        });

        const categoriesToLoad = (analysisIndex.availableCategoryArtifacts || []).map((x) => x.path);
        const categoryJson = await Promise.all(
          categoriesToLoad.map((path) => this.readJson(`../analysis/${path}`))
        );

        this.categoryArtifacts = categoryJson.sort((a, b) => {
          return this.compareCategory(
            a.analysisMetadata?.category || '',
            b.analysisMetadata?.category || ''
          );
        });

        this.allFindings = this.categoryArtifacts
          .flatMap((artifact) => {
            return (artifact.findings || []).map((finding) => ({
              ...finding,
              category: artifact.analysisMetadata?.category || 'unknown'
            }));
          })
          .sort((a, b) => {
            const severityCompare = this.compareSeverity(a.severity, b.severity);
            if (severityCompare !== 0) {
              return severityCompare;
            }

            const riskCompare = this.compareSeverity(a.aiRisk, b.aiRisk);
            if (riskCompare !== 0) {
              return riskCompare;
            }

            if (a.category !== b.category) {
              return this.compareCategory(a.category, b.category);
            }

            return a.title.localeCompare(b.title);
          });
      } catch (error) {
        this.errorMessage = error instanceof Error ? error.message : String(error);
      } finally {
        this.loading = false;
      }
    },

    get filteredFindings() {
      const search = this.filters.search.trim().toLowerCase();

      return this.allFindings.filter((finding) => {
        if (this.filters.category && finding.category !== this.filters.category) {
          return false;
        }

        if (this.filters.severity && finding.severity !== this.filters.severity) {
          return false;
        }

        if (this.filters.risk && finding.aiRisk !== this.filters.risk) {
          return false;
        }

        if (this.filters.findingsConfidence && finding.confidence !== this.filters.findingsConfidence) {
          return false;
        }

        if (this.filters.findingsFalsePositiveRisk && finding.falsePositiveRisk !== this.filters.findingsFalsePositiveRisk) {
          return false;
        }

        if (!search) {
          return true;
        }

        const corpus = [
          finding.title,
          finding.impact,
          finding.whyItMatters,
          ...(finding.evidence || [])
        ]
          .join(' ')
          .toLowerCase();

        return corpus.includes(search);
      });
    },

    get allInventory() {
      return this.categoryArtifacts
        .flatMap((artifact) => {
          const category = artifact.analysisMetadata?.category || 'unknown';
          return (artifact.inventory || []).map((item) => ({
            ...item,
            category
          }));
        })
        .sort((a, b) => {
          const categoryCompare = this.compareCategory(a.category, b.category);
          if (categoryCompare !== 0) {
            return categoryCompare;
          }

          const typeCompare = (a.componentType || '').localeCompare(b.componentType || '');
          if (typeCompare !== 0) {
            return typeCompare;
          }

          return (a.name || '').localeCompare(b.name || '');
        });
    },

    get componentTypes() {
      return [...new Set(this.allInventory.map((item) => item.componentType).filter(Boolean))].sort((a, b) => a.localeCompare(b));
    },

    get filteredInventory() {
      const search = this.filters.search.trim().toLowerCase();

      return this.allInventory.filter((item) => {
        if (this.filters.category && item.category !== this.filters.category) {
          return false;
        }

        if (this.filters.componentType && item.componentType !== this.filters.componentType) {
          return false;
        }

        if (!search) {
          return true;
        }

        const corpus = [
          item.componentType,
          item.name,
          item.identifier,
          item.registration?.mechanism,
          item.registration?.location,
          ...(item.files || []),
          ...(item.relatedFiles || [])
        ]
          .join(' ')
          .toLowerCase();

        return corpus.includes(search);
      });
    },

    get categorySnapshots() {
      return this.categoryArtifacts.map((artifact) => {
        const category = artifact.analysisMetadata?.category || 'unknown';
        return {
          category,
          coverageStatus: artifact.coverage?.status || 'unknown',
          coverageSummary: artifact.coverage?.summary || '',
          analyzedTypes: artifact.coverage?.analyzedComponentTypes || [],
          missingTypes: artifact.coverage?.missingComponentTypes || [],
          findingsCount: (artifact.findings || []).length,
          recommendationsCount: (artifact.recommendations || []).length,
          inventoryCount: (artifact.inventory || []).length,
          docsReferences: artifact.analysisMetadata?.docsReferences || []
        };
      }).sort((a, b) => this.compareCategory(a.category, b.category));
    },

    get resolvedActions() {
      const summaryActions = this.summary?.prioritizedActions || [];
      if (summaryActions.length > 0) {
        return summaryActions;
      }

      const fallback = this.categoryArtifacts.flatMap((artifact) => {
        const category = artifact.analysisMetadata?.category || 'unknown';
        return (artifact.recommendations || []).map((recommendation, index) => ({
          id: `ACT-${String(index + 1).padStart(3, '0')}`,
          summary: recommendation.summary || '',
          categories: [category],
          sourceRecommendations: [
            {
              category,
              recommendationIndex: index
            }
          ],
          priority: 'P3',
          remediationRisk: recommendation.remediationRisk || 'Medium',
          estimatedAgentEffort: recommendation.estimatedAgentEffort || 'Medium',
          confidence: recommendation.confidence || 'Medium',
          docReferences: recommendation.docReferences || []
        }));
      });

      return fallback.sort((a, b) => {
        const categoryCompare = this.compareCategory(a.categories?.[0] || '', b.categories?.[0] || '');
        if (categoryCompare !== 0) {
          return categoryCompare;
        }
        return a.summary.localeCompare(b.summary);
      }).map((action, idx) => ({
        ...action,
        id: `ACT-${String(idx + 1).padStart(3, '0')}`
      }));
    },

    get filteredActions() {
      const actions = this.resolvedActions;
      return actions.filter((action) => {
        if (this.filters.category && !(action.categories || []).includes(this.filters.category)) {
          return false;
        }

        if (this.filters.actionPriority && action.priority !== this.filters.actionPriority) {
          return false;
        }

        if (this.filters.actionRemediationRisk && action.remediationRisk !== this.filters.actionRemediationRisk) {
          return false;
        }

        if (this.filters.actionEffort && action.estimatedAgentEffort !== this.filters.actionEffort) {
          return false;
        }

        if (this.filters.actionConfidence && action.confidence !== this.filters.actionConfidence) {
          return false;
        }

        return true;
      });
    },

    get summaryMetrics() {
      const metrics = this.summary?.metrics || {};
      const findingCount = Number(metrics.findingCount || 0);
      const recommendationCount = Number(metrics.recommendationCount || 0);
      const consistencyCheckCount = Number(metrics.consistencyCheckCount || 0);
      const consistencyPassRate = Number(metrics.consistencyPassRate || 0);

      return {
        findingCount,
        recommendationCount,
        consistencyCheckCount,
        consistencyPassRate,
        passRatePercent: `${Math.round(consistencyPassRate * 100)}%`,
        severityCounts: metrics.severityCounts || {},
        aiRiskCounts: metrics.aiRiskCounts || {}
      };
    },

    get topRisks() {
      const items = this.summary?.topRisks || [];

      return [...items].sort((a, b) => {
        const severityCompare = this.compareSeverity(a.severity, b.severity);
        if (severityCompare !== 0) {
          return severityCompare;
        }

        const riskCompare = this.compareSeverity(a.aiRisk, b.aiRisk);
        if (riskCompare !== 0) {
          return riskCompare;
        }

        const categoryCompare = this.compareCategory(a.category || '', b.category || '');
        if (categoryCompare !== 0) {
          return categoryCompare;
        }

        return (a.title || '').localeCompare(b.title || '');
      });
    },

    get filteredTopRisks() {
      const search = this.filters.search.trim().toLowerCase();

      return this.topRisks.filter((risk) => {
        if (this.filters.category && risk.category !== this.filters.category) {
          return false;
        }

        if (this.filters.severity && risk.severity !== this.filters.severity) {
          return false;
        }

        if (this.filters.risk && risk.aiRisk !== this.filters.risk) {
          return false;
        }

        if (this.filters.topFalsePositiveRisk && risk.falsePositiveRisk !== this.filters.topFalsePositiveRisk) {
          return false;
        }

        if (this.filters.topEffort && risk.estimatedAgentEffort !== this.filters.topEffort) {
          return false;
        }

        if (!search) {
          return true;
        }

        const corpus = [risk.title, ...(risk.evidence || [])]
          .join(' ')
          .toLowerCase();

        return corpus.includes(search);
      });
    },

    get allConsistencyChecks() {
      return this.categoryArtifacts
        .flatMap((artifact) => {
          const category = artifact.analysisMetadata?.category || 'unknown';
          return (artifact.consistencyChecks || []).map((check) => ({
            ...check,
            category
          }));
        })
        .sort((a, b) => {
          const statusRank = { fail: 0, warning: 1, pass: 2 };
          const statusCompare = (statusRank[a.status] ?? 99) - (statusRank[b.status] ?? 99);
          if (statusCompare !== 0) {
            return statusCompare;
          }

          const importanceCompare = this.compareSeverity(this.toLevel(a.importance), this.toLevel(b.importance));
          if (importanceCompare !== 0) {
            return importanceCompare;
          }

          const categoryCompare = this.compareCategory(a.category || '', b.category || '');
          if (categoryCompare !== 0) {
            return categoryCompare;
          }

          return (a.title || '').localeCompare(b.title || '');
        });
    },

    get filteredConsistencyChecks() {
      const search = this.filters.search.trim().toLowerCase();

      return this.allConsistencyChecks.filter((check) => {
        if (this.filters.category && check.category !== this.filters.category) {
          return false;
        }

        if (this.filters.checkStatus && check.status !== this.filters.checkStatus.toLowerCase()) {
          return false;
        }

        if (this.filters.checkImportance && this.toLevel(check.importance) !== this.filters.checkImportance) {
          return false;
        }

        if (!search) {
          return true;
        }

        const corpus = [check.title, check.details, ...(check.evidence || [])]
          .join(' ')
          .toLowerCase();

        return corpus.includes(search);
      });
    },

    coverageBadgeClass(status) {
      if (status === 'analyzed') {
        return 'badge-low';
      }

      if (status === 'partially-covered') {
        return 'badge-medium';
      }

      return 'badge-high';
    },

    severityCount(severity) {
      return this.allFindings.filter((finding) => finding.severity === severity).length;
    },

    riskCount(risk) {
      return this.allFindings.filter((finding) => finding.aiRisk === risk).length;
    },

    badgeClass(level) {
      if (level === 'P1') {
        return 'badge-high';
      }

      if (level === 'P2') {
        return 'badge-medium';
      }

      if (level === 'P3') {
        return 'badge-low';
      }

      if (level === 'High') {
        return 'badge-high';
      }

      if (level === 'Medium') {
        return 'badge-medium';
      }

      if (level === 'Low') {
        return 'badge-low';
      }

      return 'badge-neutral';
    },

    consistencyStatusBadgeClass(status) {
      if (status === 'fail') {
        return 'badge-high';
      }

      if (status === 'warning') {
        return 'badge-medium';
      }

      if (status === 'pass') {
        return 'badge-low';
      }

      return 'badge-neutral';
    },

    toLevel(value) {
      if (!value) {
        return '';
      }

      return `${value}`.charAt(0).toUpperCase() + `${value}`.slice(1).toLowerCase();
    },

    normalizeAnchorToken(value) {
      return `${value || ''}`
        .toLowerCase()
        .replaceAll(/[^a-z0-9]+/g, '-')
        .replaceAll(/^-+|-+$/g, '');
    },

    findingRowId(finding) {
      const category = this.normalizeAnchorToken(finding?.category);
      const title = this.normalizeAnchorToken(finding?.title);
      return `finding-${category}-${title}`;
    },

    riskFindingHref(risk) {
      const match = this.allFindings.find((finding) => {
        return finding.category === risk.category && finding.title === risk.title;
      });

      if (!match) {
        return '#findings-section';
      }

      return `#${this.findingRowId(match)}`;
    },

    canonicalCategoryOrder() {
      return [
        'admin-ui',
        'page-builder',
        'email-builder',
        'form-builder',
        'global-extensibility'
      ];
    },

    compareCategory(a, b) {
      const order = this.canonicalCategoryOrder();
      return order.indexOf(a) - order.indexOf(b);
    },

    compareSeverity(a, b) {
      const rank = { High: 0, Medium: 1, Low: 2 };
      return (rank[a] ?? 99) - (rank[b] ?? 99);
    },

    inventoryRowKey(category, componentType, identifier, index) {
      return `${category}-${componentType}-${identifier || 'unknown'}-${index}`;
    },

    isInventoryRowExpanded(key) {
      return Boolean(this.expandedInventoryRows[key]);
    },

    toggleInventoryRow(key) {
      this.expandedInventoryRows = {
        ...this.expandedInventoryRows,
        [key]: !this.expandedInventoryRows[key]
      };
    },

    shortPath(path) {
      if (!path) {
        return '-';
      }

      const normalized = path.replaceAll('\\\\', '/');
      const parts = normalized.split('/');
      if (parts.length <= 3) {
        return normalized;
      }

      return `.../${parts.slice(-3).join('/')}`;
    },

    objectEntries(input) {
      if (!input || typeof input !== 'object') {
        return [];
      }

      return Object.entries(input).map(([key, value]) => ({
        key,
        value: typeof value === 'string' ? value : JSON.stringify(value)
      }));
    },

    async readJson(path) {
      const response = await fetch(path, { cache: 'no-store' });
      if (!response.ok) {
        throw new Error(`Failed to read ${path}: ${response.status} ${response.statusText}`);
      }

      return response.json();
    }
  };
}
