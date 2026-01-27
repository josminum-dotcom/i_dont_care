using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using LogExtractorCore;

namespace LogExtractorApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnSelectExcel_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtExcelPath.Text = ofd.FileName;
                }
            }
        }

        private void btnSelectLog_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Log Files (*.log)|*.log|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtLogPath.Text = ofd.FileName;
                }
            }
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            string excelPath = txtExcelPath.Text;
            string logPath = txtLogPath.Text;

            if (string.IsNullOrEmpty(excelPath) || !File.Exists(excelPath))
            {
                MessageBox.Show("엑셀 파일을 선택해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrEmpty(logPath) || !File.Exists(logPath))
            {
                MessageBox.Show("로그 파일을 선택해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btnStart.Enabled = false;
            lblStatus.Text = "엑셀 분석 중...";
            progressBar1.Value = 0;

            try
            {
                string excelName = Path.GetFileNameWithoutExtension(excelPath);
                string outputFolder = Path.Combine(Path.GetDirectoryName(excelPath)!, excelName);

                var parser = new ExcelParser();
                var testCases = await Task.Run(() => parser.Parse(excelPath));

                if (testCases.Count == 0)
                {
                    MessageBox.Show("분석된 테스트 케이스가 없습니다. 시트 이름과 형식을 확인해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    btnStart.Enabled = true;
                    return;
                }

                lblStatus.Text = $"로그 추출 중... (총 {testCases.Count}개 케이스)";

                var progress = new Progress<int>(v =>
                {
                    progressBar1.Value = v;
                });

                var processor = new LogProcessor();
                await Task.Run(() => processor.Process(logPath, testCases, outputFolder, progress));

                lblStatus.Text = "작업 완료!";
                MessageBox.Show($"작업이 완료되었습니다.\n저장 위치: {outputFolder}", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "오류 발생";
            }
            finally
            {
                btnStart.Enabled = true;
            }
        }
    }
}
